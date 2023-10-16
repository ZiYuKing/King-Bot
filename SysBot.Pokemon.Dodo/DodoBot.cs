using System.Threading;
using System.Threading.Tasks;
using DoDo.Open.Sdk.Models;
using System.Linq;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Models.Personals;
using DoDo.Open.Sdk.Models.ChannelMessages;
using DoDo.Open.Sdk.Models.Islands;
using DoDo.Open.Sdk.Services;
using PKHeX.Core;
using System.Collections.Generic;
using System.Threading.Channels;
using System;
using SysBot.Base;
using System.Security.Cryptography;

namespace SysBot.Pokemon.Dodo
{
    public class DodoBot<T> where T : PKM, new()
    {
        private static PokeTradeHub<T> Hub = default!;
        internal static TradeQueueInfo<T> Info => Hub.Queues.Info;

        public static OpenApiService OpenApiService = default!;

        private static DodoSettings Settings = default!;

        public DodoBot(DodoSettings settings, PokeTradeHub<T> hub)
        {
            Hub = hub;
            Settings = settings;
            //开放接口服务
            OpenApiService = new OpenApiService(new OpenApiOptions
            {
                BaseApi = settings.BaseApi,
                ClientId = settings.ClientId,
                Token = settings.Token
            });
            //事件处理服务，可自定义，只要继承EventProcessService抽象类即可
            var eventProcessService = new PokemonProcessService<T>(OpenApiService, settings);
            //开放事件服务
            var openEventService = new OpenEventService(OpenApiService, eventProcessService, new OpenEventOptions
            {
                IsReconnect = true,
                IsAsync = true
            });
            //接收事件消息
            Task.Run(async () =>
            {
                StartDistribution();
                await openEventService.ReceiveAsync();

            });
        }

        public void StartDistribution()
        {
            var channelId = Settings.ChannelId;
            EchoUtil.Forwarders.Add(msg =>{ if (msg.StartsWith("https")) SendChannelMessagePicture(msg, channelId);});
            EchoUtil.Forwarders.Add(msg => { if (msg.Contains("团")) SendChannelMessage(msg, channelId); });
            EchoUtil.Forwarders.Add(msg => { if (msg.Contains("打")) SendChannelMessage(msg, channelId); });
            if (string.IsNullOrWhiteSpace(channelId)) return;
            SendChannelAtVIPMessage("欢迎使用King-交换机器人！", channelId);
            //SendChannelMessageAll("欢迎使用传火机器人！", channelId);
            var Msg = "";
            if (!DodoBot<T>.Info.Hub.Config.Legality.AllowUseFile)
            {
                Msg = $"本频道不允许上传文件";
            }
            else
            {
                Msg = "本频道可以上传文件";
            }
            Task.Delay(1_000).ConfigureAwait(false);


            if (typeof(T) == typeof(PK8))
            {
                SendChannelMessage($"当前版本为剑盾,{Msg}", channelId);
            }
            else if (typeof(T) == typeof(PB8))
            {
                SendChannelMessage($"当前版本为晶灿钻石明亮珍珠,{Msg}", channelId);
            }
            else if (typeof(T) == typeof(PA8))
            {
                SendChannelMessage($"当前版本为阿尔宙斯,{Msg}", channelId);
            }
            else if (typeof(T) == typeof(PK9))
            {
                SendChannelMessage($"当前版本为朱紫,{Msg}", channelId);
            }
        }

        public static void SendChannelMessage(string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = message
                }
            });
        }

        public static void SendChannelAtMessage(ulong atDodoId, string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = $"<@!{atDodoId}> {message}"
                }
            });
        }
        public static void SendChannelAtVIPMessage(string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = $"<@&{173664}><@&{171954}> {message}"
                }
            });
        }


        public static void SendChannelMessageAll(string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = $"<@online> {message}"
                }
            });
        }

        public static void SendPersonalMessage(string dodoId, string message, string islandSourceId = "")
        {
            if (string.IsNullOrEmpty(message)) return;
            if (string.IsNullOrWhiteSpace(islandSourceId))
            {
                islandSourceId = OpenApiService.GetIslandList(new GetIslandListInput()).FirstOrDefault().IslandSourceId ?? "";
            }
            OpenApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyText>
            {
                IslandSourceId = islandSourceId,
                DodoSourceId = dodoId,
                MessageBody = new MessageBodyText
                {
                    Content = message
                }
            });
        }
        public static void SendChannelMessagePicture(string url, string channelId)
        {
            if (string.IsNullOrEmpty(url)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyPicture>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyPicture
                {
                    Url = url,
                    Width = 1280,
                    Height = 720,
                    IsOriginal = 1
                }
            });
        }
        public static void SendPersonalMessagePicture(string url, string dodoId, string islandSourceId = "")
        {
            if (string.IsNullOrEmpty(url)) return;
            if (string.IsNullOrWhiteSpace(islandSourceId))
            {
                islandSourceId = OpenApiService.GetIslandList(new GetIslandListInput()).FirstOrDefault().IslandSourceId ?? "";
            }
            OpenApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyPicture>
            {
                IslandSourceId = islandSourceId,
                DodoSourceId = dodoId,
                MessageBody = new MessageBodyPicture
                {
                    Url = url,
                    Width = 1280,
                    Height = 720,
                    IsOriginal = 1
                }
            });
        }

        public class ColorGenerator
        {
            private static readonly string[] colors = { "black", "purple", "blue", "indigo", "green", "yellow", "orange", "red" };
            private static List<string> availableColors = new List<string>(colors);
            private static int currentIndex;

            public static string RandomColor
            {
                get
                {
                    if (availableColors.Count == 0)
                    {
                        ResetAvailableColors();
                    }

                    currentIndex = RandomNumberGenerator.GetInt32(availableColors.Count);
                    string randomColor = availableColors[currentIndex];
                    availableColors.RemoveAt(currentIndex);

                    return randomColor;
                }
            }

            private static void ResetAvailableColors()
            {
                availableColors = new List<string>(colors);
                currentIndex = 0;
            }
        }
        public static class PokemonColors
        {
            public static string GetColor(string pokemonType)
            {
                return pokemonType switch
                {
                    "Normal" => "grey",
                    "Fighting" => "orange",
                    "Flying" => "indigo",
                    "Poison" => "purple",
                    "Ground" => "orange",
                    "Rock" => "grey",
                    "Bug" => "green",
                    "Ghost" => "purple",
                    "Steel" => "indigo",
                    "Fire" => "red",
                    "Water" => "blue",
                    "Grass" => "green",
                    "Electric" => "yellow",
                    "Psychic" => "red",
                    "Ice" => "indigo",
                    "Dragon" => "blue",
                    "Dark" => "black",
                    "Fairy" => "red",
                    _ => "default"
                };
            }
        }

        public static void SendChannelCardMessage(string message, string channelId, string pokeurl,string itemurl, string ballurl,string teraurl, string teraoriginalurl, string shinyurl, string movetypeurl1, string movetypeurl2, string movetypeurl3, string movetypeurl4)
        {
            string mes = "";
            if (string.IsNullOrEmpty(message)) message= "None";
            string[] pmsgLines = message.Split('\n');
            mes = pmsgLines[4]+"\n"+ pmsgLines[2] + "\n" + pmsgLines[3] + "\n" + pmsgLines[7] + "\n" + pmsgLines[8] + "\n" + pmsgLines[14] + "\n" + pmsgLines[15] + "\n" + pmsgLines[5] + "\n";
            
            string[] parts = pmsgLines[7].Split(',');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Replace(":", "\n");

            string[] part = pmsgLines[8].Split(',');
            for (int i = 0; i < part.Length; i++)
                part[i] = part[i].Replace(":", "\n");

            var Gender = $"{pmsgLines[1]}";
            if (Gender == "-")
            {
                Gender = "(无性别)";
            }
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyCard>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyCard
                {

                    Card = new MessageModelCard
                    {
                        Type = "card",
                        Theme = PokemonColors.GetColor("PokemonType"),
                        //Title = "这是你要的宝可梦：",
                        Components = new List<object>
                        {
                            new
                            {
                                type = "section",
                                text = new
                                {
                                    type = "dodo-md",
                                    content = $"**宝可梦：{pmsgLines[0] + Gender}**"
                                },
                            },
                            new
                            {
                                type = "remark",
                                elements = new List<object>()
                                {
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"{pmsgLines[16]}"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = ballurl
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"{pmsgLines[17]}"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = itemurl
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = "https://img.imdodo.com/openapitest/upload/cdn/4A47A0DB6E60853DEDFCFDF08A5CA249_1695595586219.png"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = shinyurl
                                    },
                                }
                            },
                             new
                            {
                                type = "remark",
                                elements = new List<object>()
                                {
                                    new
                                    {
                                        type = "dodo-md",
                                       content = "原始太晶属性:"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = teraoriginalurl
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = "当前太晶属性:"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = teraurl
                                    }

                                }
                            },
                            new
                            {
                                type = "section",
                                text = new
                                {
                                    type = "dodo-md",
                                    content = mes
                                },
                                align = "right",
                                accessory = new
                                {
                                      type = "image",
                                      src = pokeurl
                                }
                            },
                            new
                            {
                                  type = "remark",
                                elements = new List<object>()
                                {
                                    new
                                    {
                                        type = "image",
                                        src = movetypeurl1
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"**{pmsgLines[9]}         **"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = movetypeurl2
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"**{pmsgLines[10]}**"
                                    },
                                }
                            },
                            new
                            {
                                  type = "remark",
                                elements = new List<object>()
                                {
                                    new
                                    {
                                        type = "image",
                                        src = movetypeurl3
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"**{pmsgLines[11]}         **"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = movetypeurl4
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"**{pmsgLines[12]}**"
                                    }
                                }
                            }
                        }
                    }
                }
            }); 
        }
        public static void SendChannelEggCardMessage(string title, string message, string channelId, string pokeurl, string ballurl, string shinyurl, string shinyinfo)
        {
            string mes = "";
            if (string.IsNullOrEmpty(message)) message = "None";
            if (string.IsNullOrEmpty(title)) title = "None";
            if (string.IsNullOrEmpty(shinyinfo)) shinyinfo = "None";
            string[] pmsgLines = message.Split('\n');
            string[] parts = pmsgLines[8].Split(',');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Replace(":", "\n");
            mes = pmsgLines[0] + "\n" + pmsgLines[1] + "\n" + pmsgLines[2] + "\n" + pmsgLines[3] + "\n" +  shinyinfo;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyCard>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyCard
                {

                    Card = new MessageModelCard
                    {
                        Type = "card",
                        Theme = ColorGenerator.RandomColor,
                        Title = title,
                        Components = new List<object>
                        {
                            new
                            {
                                type = "remark",
                                elements = new List<object>()
                                {
                                    new
                                    {
                                        type = "image",
                                        src = shinyurl
                                    },
                                     new
                                    {
                                        type = "image",
                                        src = "https://img.imdodo.com/openapitest/upload/cdn/E714716E359055F4AD802BD414A97AF2_1696061162810.png"
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = "球种:"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = ballurl
                                    }
                                }
                            },
                            new
                            {
                                type = "section",
                                text = new
                                {
                                    type = "dodo-md",
                                    content = mes
                                },
                                align = "left",
                                accessory = new
                                {
                                      type = "image",
                                      src = pokeurl
                                }
                            },
                              new
                            {
                              type = "section",
                                text = new
                                {
                                    type = "dodo-md",
                                    content = "**个体：**"
                                }
                            },
                             new
                            {
                              type = "section",
                                text = new
                                {
                                    type = "paragraph",
                                    cols = 6,
                                    fields = new List<object>()
                                    {
                                         new
                                        {
                                            type = "dodo-md",
                                           content = parts[0]
                                        },
                                         new
                                        {
                                            type = "dodo-md",
                                           content =  parts[1]
                                        },
                                         new
                                        {
                                            type = "dodo-md",
                                           content = parts[2]
                                        },
                                         new
                                        {
                                            type = "dodo-md",
                                           content = parts[3]
                                        },
                                           new
                                        {
                                            type = "dodo-md",
                                           content = parts[4]
                                        },
                                             new
                                        {
                                            type = "dodo-md",
                                           content = parts[5]
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            });
        }
        public static void SendChannelCardBatchMessage(string message, string channelId, string pokeurl, string itemurl, string ballurl, string shinyurl)
        {
            string[] pmsgLines = message.Split('\n');
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyCard>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyCard
                {

                    Card = new MessageModelCard
                    {
                        Type = "card",
                        Theme = ColorGenerator.RandomColor,
                        Components = new List<object>
                        {
                            new
                            {
                                type = "remark",
                                elements = new List<object>()
                                {
                                    new
                                    {
                                        type = "image",
                                        src = shinyurl
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = pokeurl
                                    },
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"**{pmsgLines[0]}**"
                                    },
                                    
                                    new
                                    {
                                        type = "dodo-md",
                                       content = $"携带:{pmsgLines[17]}"
                                    },
                                    new
                                    {
                                        type = "image",
                                        src = itemurl
                                    }
                                }
                            }
                                                
                        }
                    }
                }
            });
        }
        public static void SendChannelCardTimeMessage(string message, string channelId)
        {
            long SetTime = (Hub.Config.Trade.TradeWaitTime * 1_000) + 87_000;
            long endtime = DateTime.Now.AddMilliseconds(SetTime).GetTimeStamp();
            string[] pmsgLines = message.Split('\n');
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyCard>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyCard
                {

                    Card = new MessageModelCard
                    {
                        Type = "card",
                        Title = "开始连接倒计时",
                        Theme = ColorGenerator.RandomColor,
                        Components = new List<object>
                        {
                            new
                            {
                                type = "countdown",
                                title = $"正在交换：{pmsgLines[0]}",
                                style = "hour",
                                endTime = endtime
                            },

                        }
                    }
                }
            });
        }

    }
}