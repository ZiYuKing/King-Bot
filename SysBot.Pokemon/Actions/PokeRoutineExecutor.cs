﻿using PKHeX.Core;
using SysBot.Base;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Globalization;
using static SysBot.Base.SwitchButton;


namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor<T> : PokeRoutineExecutorBase where T : PKM, new()
    {
        protected PokeRoutineExecutor(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> cfg) : base(cfg)
        {
        }

        public abstract Task<T> ReadPokemon(ulong offset, CancellationToken token);
        public abstract Task<T> ReadPokemon(ulong offset, int size, CancellationToken token);
        public abstract Task<T> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token);
        public abstract Task<T> ReadBoxPokemon(int box, int slot, CancellationToken token);

        public async Task<T?> ReadUntilPresent(ulong offset, int waitms, int waitInterval, int size, CancellationToken token)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var pk = await ReadPokemon(offset, size, token).ConfigureAwait(false);
                if (pk.Species != 0 && pk.ChecksumValid)
                    return pk;
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }
            return null;
        }

        public async Task<T?> ReadUntilPresentPointer(IReadOnlyList<long> jumps, int waitms, int waitInterval, int size, CancellationToken token)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var pk = await ReadPokemonPointer(jumps, size, token).ConfigureAwait(false);
                if (pk.Species != 0 && pk.ChecksumValid)
                    return pk;
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }
            return null;
        }

        protected async Task<(bool, ulong)> ValidatePointerAll(IEnumerable<long> jumps, CancellationToken token)
        {
            var solved = await SwitchConnection.PointerAll(jumps, token).ConfigureAwait(false);
            return (solved != 0, solved);
        }

        public static void DumpPokemon(string folder, string subfolder, T pk)
        {
            if (!Directory.Exists(folder))
                return;
            var dir = Path.Combine(folder, subfolder);
            Directory.CreateDirectory(dir);
            var fn = Path.Combine(dir, Util.CleanFileName(pk.FileName));
            File.WriteAllBytes(fn, pk.DecryptedPartyData);
            LogUtil.LogInfo($"已保存文件: {fn}", "Dump");
        }

        public async Task<bool> TryReconnect(int attempts, int extraDelay, SwitchProtocol protocol, CancellationToken token)
        {
            // USB can have several reasons for connection loss, some of which is not recoverable (power loss, sleep). Only deal with WiFi for now.
            if (protocol is SwitchProtocol.WiFi)
            {
                // If ReconnectAttempts is set to -1, this should allow it to reconnect (essentially) indefinitely.
                for (int i = 0; i < (uint)attempts; i++)
                {
                    LogUtil.LogInfo($"正在尝试重新连接... ({i + 1})", Connection.Label);
                    Connection.Reset();
                    if (Connection.Connected)
                        break;

                    await Task.Delay(30_000 + extraDelay, token).ConfigureAwait(false);
                }
            }
            return Connection.Connected;
        }

        public async Task VerifyBotbaseVersion(CancellationToken token)
        {
            var data = await SwitchConnection.GetBotbaseVersion(token).ConfigureAwait(false);
            var version = decimal.TryParse(data, CultureInfo.InvariantCulture, out var v) ? v : 0;
            if (version < BotbaseVersion)
            {
                var protocol = Config.Connection.Protocol;
                var msg = protocol is SwitchProtocol.WiFi ? "sys-botbase" : "usb-botbase";
                msg += $" version is not supported. Expected version {BotbaseVersion} or greater, and current version is {version}. Please download the latest version from: ";
                if (protocol is SwitchProtocol.WiFi)
                    msg += "https://github.com/olliz0r/sys-botbase/releases/latest";
                else
                    msg += "https://github.com/Koi-3088/usb-botbase/releases/latest";
                throw new Exception(msg);
            }
        }

        // Check if either Tesla or dmnt are active if the sanity check for Trainer Data fails, as these are common culprits.
        private const ulong ovlloaderID = 0x420000000007e51a; // Tesla Menu
        private const ulong dmntID = 0x010000000000000d;      // dmnt used for cheats

        public async Task CheckForRAMShiftingApps(CancellationToken token)
        {
            Log("Trainer data is not valid.");

            bool found = false;
            var msg = "";
            if (await SwitchConnection.IsProgramRunning(ovlloaderID, token).ConfigureAwait(false))
            {
                msg += "Found Tesla Menu";
                found = true;
            }

            if (await SwitchConnection.IsProgramRunning(dmntID, token).ConfigureAwait(false))
            {
                if (found)
                    msg += " and ";
                msg += "dmnt (cheat codes?)";
                found = true;
            }
            if (found)
            {
                msg += ".";
                Log(msg);
                Log("Please remove interfering applications and reboot the Switch.");
            }
        }

        protected async Task<PokeTradeResult> CheckPartnerReputation(PokeRoutineExecutor<T> bot, PokeTradeDetail<T> poke, ulong TrainerNID, string TrainerName,
            TradeAbuseSettings AbuseSettings, TrackedUserLog PreviousUsers, TrackedUserLog PreviousUsersDistribution, TrackedUserLog EncounteredUsers, CancellationToken token)
        {
            bool quit = false;
            var user = poke.Trainer;
            var isDistribution = poke.Type == PokeTradeType.Random;
            var useridmsg = isDistribution ? "" : $" ({user.ID})";
            var list = isDistribution ? PreviousUsersDistribution : PreviousUsers;

            // Matches to a list of banned NIDs, in case the user ever manages to enter a trade.
            var entry = AbuseSettings.BannedIDs.List.Find(z => z.ID == TrainerNID);
            if (entry != null)
            {
                if (AbuseSettings.BlockDetectedBannedUser && bot is PokeRoutineExecutor8SWSH)
                    await BlockUser(token).ConfigureAwait(false);

                var msg = $"{user.TrainerName}{useridmsg} 是一个黑名单的用户，并且在游戏中使用OT: {TrainerName}.";
                if (!string.IsNullOrWhiteSpace(entry.Comment))
                    msg += $"\n用户因以下原因被禁: {entry.Comment}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.BannedIDMatchEchoMention))
                    msg = $"{AbuseSettings.BannedIDMatchEchoMention} {msg}";
                //ReBannedList<PokeTradeBotSWSH>.ReBL($"连接到黑名单用户:{user.TrainerName}OT: {TrainerName}NID:{TrainerNID}该用户因以下原因被禁:{entry.Comment}");
                EchoUtil.Echo(msg);
                return PokeTradeResult.SuspiciousActivity;
            }

            // Allows setting a cooldown for repeat trades. If the same user is encountered within the cooldown period, the user is warned and the trade will be ignored.
            var cooldown = list.TryGetPrevious(TrainerNID);
            if (cooldown != null)
            {
                var delta = DateTime.Now - cooldown.Time;
                Log($"在 {delta.TotalMinutes:F1} 分钟前连接过：{user.TrainerName}(游戏名称: {TrainerName})");
               
                var cd = AbuseSettings.TradeCooldown;
                if (cd != 0 && TimeSpan.FromMinutes(cd) > delta)
                {
                    poke.Notifier.SendNotification(bot, poke, $"你仍处在交换冷却时间中, 冷却时间还剩：{TimeSpan.FromMinutes(cd) - delta} 分钟.");
                    var msg = $"Found {user.TrainerName}{useridmsg} 无视 {cd} 分钟交易冷却时间.在 {delta.TotalMinutes:F1} 分钟前连接过.";
                    if (AbuseSettings.EchoNintendoOnlineIDCooldown)
                        msg += $"\nID: {TrainerNID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.CooldownAbuseEchoMention))
                        msg = $"{AbuseSettings.CooldownAbuseEchoMention} {msg}";
                    EchoUtil.Echo(msg);
                    return PokeTradeResult.SuspiciousActivity;
                }
            }

            // For non-Distribution trades, we can optionally flag users sending to multiple in-game players.
            // Can trigger if the user gets sniped, but can also catch abusers sending to many people.
            if (!isDistribution)
            {
                var previousEncounter = EncounteredUsers.TryRegister(poke.Trainer.ID, TrainerName, poke.Trainer.ID);
                if (previousEncounter != null && previousEncounter.Name != TrainerName)
                {
                    if (AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                    {
                        if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                        {
                            await BlockUser(token).ConfigureAwait(false);
                            if (AbuseSettings.BanIDWhenBlockingUser || bot is not PokeRoutineExecutor8SWSH) // Only ban ID if blocking in SWSH, always in other games.
                            {
                                AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "给多个游戏存档发送游戏数据") });
                                Log($"已经将 {TrainerNID} 加入黑名单.");
                            }
                        }
                        quit = true;
                    }

                    var msg = $"发现 {user.TrainerName}{useridmsg} 使用多个游戏存档交换.上一个角色OT: {previousEncounter.Name}, C当前角色OT: {TrainerName}";
                    if (AbuseSettings.EchoNintendoOnlineIDMultiRecipients)
                        msg += $"\nID: {TrainerNID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiRecipientEchoMention))
                        msg = $"{AbuseSettings.MultiRecipientEchoMention} {msg}";
                    EchoUtil.Echo(msg);
                }
            }

            if (quit)
                return PokeTradeResult.SuspiciousActivity;

            // Try registering the partner in our list of recently seen.
            // Get back the details of their previous interaction.
            var previous = isDistribution
                ? list.TryRegister(TrainerNID, TrainerName)
                : list.TryRegister(TrainerNID, TrainerName, poke.Trainer.ID);
            // For non-Distribution trades, flag users using multiple Discord/Twitch accounts to send to the same in-game player.
            // This is usually to evade a ban or a trade cooldown.
            if (previous != null && previous.NetworkID == TrainerNID && previous.RemoteID != user.ID && !isDistribution)
            {
                var delta = DateTime.Now - previous.Time;
                if (delta < TimeSpan.FromMinutes(AbuseSettings.TradeAbuseExpiration) && AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                {
                    if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                    {
                        await BlockUser(token).ConfigureAwait(false);
                        if (AbuseSettings.BanIDWhenBlockingUser || bot is not PokeRoutineExecutor8SWSH) // Only ban ID if blocking in SWSH, always in other games.
                        {
                            AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "使用多个账户发送游戏数据") });
                            Log($"Added {TrainerNID} to the BannedIDs list.");
                        }
                    }
                    quit = true;
                }

                var msg = $"Found {user.TrainerName}{useridmsg} 使用多个账户.\n{delta.TotalMinutes:F1}分钟前识别到{previous.Name} ({previous.RemoteID})OT: {TrainerName}.";
                if (AbuseSettings.EchoNintendoOnlineIDMulti)
                    msg += $"\nID: {TrainerNID}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiAbuseEchoMention))
                    msg = $"{AbuseSettings.MultiAbuseEchoMention} {msg}";
                EchoUtil.Echo(msg);
            }

            if (quit)
                return PokeTradeResult.SuspiciousActivity;


            return PokeTradeResult.Success;
        }

        private static RemoteControlAccess GetReference(string name, ulong id, string comment) => new()
        {
            ID = id,
            Name = name,
            Comment = $"Added automatically on {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
        };

        // Blocks a user from the box during in-game trades (SWSH).
        private async Task BlockUser(CancellationToken token)
        {
            Log("Blocking user in-game...");
            await PressAndHold(RSTICK, 0_750, 0, token).ConfigureAwait(false);
            await Click(DUP, 0_300, token).ConfigureAwait(false);
            await Click(A, 1_300, token).ConfigureAwait(false);
            await Click(A, 1_300, token).ConfigureAwait(false);
            await Click(DUP, 0_300, token).ConfigureAwait(false);
            await Click(A, 1_100, token).ConfigureAwait(false);
            await Click(A, 1_100, token).ConfigureAwait(false);
        }

        public async Task<T?> ReadUntilPresentMutiTrade(ulong offset, T lastOffered, int count, int waitms, int waitInterval, int size, CancellationToken token)
        {
            int msWaited = 0;
            if (count == 1)
            {
                while (msWaited < waitms)
                {
                    var pk = await ReadPokemon(offset, size, token).ConfigureAwait(false);
                    if (pk.Species != 0 && pk.ChecksumValid)
                        return pk;
                    await Task.Delay(waitInterval, token).ConfigureAwait(false);
                    msWaited += waitInterval;
                }
                return null;
            }
            else
            {
                var sw = new Stopwatch();
                sw.Start();
                var offered = await ReadPokemon(offset, size, token).ConfigureAwait(false);
                do
                {
                    offered = await ReadPokemon(offset, size, token).ConfigureAwait(false);
                    Log($"EC变了吗?={offered.EncryptionConstant:X}");
                    if (offered.EncryptionConstant != lastOffered.EncryptionConstant)

                        return offered;

                    await Task.Delay(waitInterval, token).ConfigureAwait(false);
                } while (sw.ElapsedMilliseconds < waitms);
                return null;
            }
        }
      

      
    }
}