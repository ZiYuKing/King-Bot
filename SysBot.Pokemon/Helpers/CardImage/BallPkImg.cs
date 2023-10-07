using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class BallPkImg
    {
        #region «Ú÷÷Õº∆¨
        public static Dictionary<int, string> ballUrlMapping = new Dictionary<int, string>
        {
            {1, "https://img.imdodo.com/openapitest/upload/cdn/4473307021693842CEE77AB2BA321262_1696440535775.png"},
            {2, "https://img.imdodo.com/openapitest/upload/cdn/6452868475C8BCE9DDA3238CCC40A668_1696440591348.png"},
            {3, "https://img.imdodo.com/openapitest/upload/cdn/79EA74BCAC28AAC5DA96F2890A4F6BFF_1696440630482.png"},
            {4, "https://img.imdodo.com/openapitest/upload/cdn/A732426BA883897926B19F7FBEE08392_1696440455853.png"},
            {5, "https://img.imdodo.com/openapitest/upload/cdn/37B39C5747AD5A92B13A3B4727998D37_1696440654792.png"},
            {6, "https://img.imdodo.com/openapitest/upload/cdn/7CDD2413BB441D2C65D69003D5BC9741_1696440690292.png"},
            {7, "https://img.imdodo.com/openapitest/upload/cdn/FB654A7AC67CDD17DA1823960F13BF22_1696478784023.png"},
            {8, "https://img.imdodo.com/openapitest/upload/cdn/6378555F3A93FBD1322BE777214285F7_1696478810878.png"},
            {9, "https://img.imdodo.com/openapitest/upload/cdn/D8CCA86A6BD7BBD64D58F7944D4E9CC7_1696478841836.png"},
            {10, "https://img.imdodo.com/openapitest/upload/cdn/6629D0BAA8EDF9D820EB52C997146E14_1696478904433.png"},
            {11, "https://img.imdodo.com/openapitest/upload/cdn/936D94D877A1AB9A898019A29265E10F_1696478937289.png"},
            {12, "https://img.imdodo.com/openapitest/upload/cdn/16E6724A93FF68363F435B264FAB5AF4_1696478964418.png"},
            {13, "https://img.imdodo.com/openapitest/upload/cdn/A9118450B0A05B9A6CEC279461983958_1696479002435.png"},
            {14, "https://img.imdodo.com/openapitest/upload/cdn/BDD540AFC18B712A22213F52C3BDA32F_1696479040152.png"},
            {15, "https://img.imdodo.com/openapitest/upload/cdn/BD62F4A3E5AFDD321F0A4C9F2FF13E83_1696479073914.png"},
            {16, "https://img.imdodo.com/openapitest/upload/cdn/AFE0ACD79326B30F75A92C883FDFA4C9_1696479107798.png"},
            {17, "https://img.imdodo.com/openapitest/upload/cdn/8D496C46AB28DFC573B50B4991185B56_1696479136050.png"},
            {18, "https://img.imdodo.com/openapitest/upload/cdn/CED72B53EEBDB457F59EAA389E71FA85_1696479182516.png"},
            {19, "https://img.imdodo.com/openapitest/upload/cdn/B3B9D381B3E8480B6C6EDA3900D4D1C2_1696479207044.png"},
            {20, "https://img.imdodo.com/openapitest/upload/cdn/89FD90A465296313DF66BC937073A08C_1696479232972.png"},
            {21, "https://img.imdodo.com/openapitest/upload/cdn/E693BC0924730A68079B7BDC11A7DACB_1696479282531.png"},
            {22, "https://img.imdodo.com/openapitest/upload/cdn/BE0B90B9CE142EAC3F8D1A41C7483597_1696479493473.png"},
            {23, "https://img.imdodo.com/openapitest/upload/cdn/E3FC372C674843611057893F4F62DF56_1696479469565.png"},
            {24, "https://img.imdodo.com/openapitest/upload/cdn/12E700EA74CE5B7CDF6E4A9D78BD5B26_1696479425495.png"},
            {25, "https://img.imdodo.com/openapitest/upload/cdn/58448A2AC730CDECFC0BBAE438837FEB_1696479392157.png"},
            {26, "https://img.imdodo.com/openapitest/upload/cdn/E3974AF293DB571FBF9D70C4FF528340_1696479365298.png"},
            {27, "https://img.imdodo.com/openapitest/upload/cdn/111E83B4424FAC7E4FEECB9A77BA8FC2_1696479767441.png"},
            {28, "https://img.imdodo.com/openapitest/upload/cdn/C265AD3FF112FE57DD6D2F9B5818F526_1695818637464.png"},
            {29, "https://img.imdodo.com/openapitest/upload/cdn/DD07B7FF63851388F5FA3775CF593D5A_1695818638004.png"},
            {30, "https://img.imdodo.com/openapitest/upload/cdn/4A550DF8D611A4AEFD5FC75E16F68FA0_1695818639076.png"},
            {31, "https://img.imdodo.com/openapitest/upload/cdn/6FC94A66998B69A296F00116D1EFBD23_1695818639686.png"},
            {32, "https://img.imdodo.com/openapitest/upload/cdn/4A6F7AEB55F7500256EBED35FE05F7EF_1695818640217.png"},
            {33, "https://img.imdodo.com/openapitest/upload/cdn/62D6AC905D9DCD3541F8DA7D58B41360_1695818640759.png"},
            {34, "https://img.imdodo.com/openapitest/upload/cdn/14912395A8826C6BCDF1F876EE181B29_1695818641238.png"},
            {35, "https://img.imdodo.com/openapitest/upload/cdn/8CB33786BC7136AF7EFFB53D640FAB18_1695818641893.png"},
            {36, "https://img.imdodo.com/openapitest/upload/cdn/1B63464BEA7CC7105FE5360357A01FEF_1695818642783.png"},
            {37, "https://img.imdodo.com/openapitest/upload/cdn/8219D752C646B30F43F401FDFCEC52A0_1695818643317.png"},

        };
        #endregion
    }
}

