using System;
using System.Drawing;
using GTA.Math;
using GTA.Native;

namespace MethOperation
{
    public static class Constants
    {
        public static readonly Random RandomGenerator = new Random();

        public const int MaxStringLength = 99;
        public const int UpdateInterval = 250;
        public const int MissionUpdateInterval = 1000;

        public const float MarkerDrawDistance = 20f;
        public const float MarkerInteractionDistance = 1.25f;
        public const float MethLabHeading = 262.4538f;

        public static readonly Vector3 MarkerScale = new Vector3(1f, 1f, 1f);
        public static readonly Vector3 MissionMarkerScale = new Vector3(8f, 8f, 1.5f);
        public static readonly Vector3 MethLabExit = new Vector3(997.2960f, -3200.676f, -37.3937f);
        public static readonly Vector3 MethLabLaptop = new Vector3(1001.9387f, -3195.216f, -39.9931f);

        public static readonly Vector3 LeanAreaMin = new Vector3(998.5406f, -3198.149f, -36.0f);
        public static readonly Vector3 LeanAreaMax = new Vector3(999.65f, -3202.0f, -37.39315f);
        public static readonly Vector3 LeanPos = new Vector3(999.03f, -3200.07f, -36.39f);
        public static readonly float LeanHeading = 270.0f;

        public static readonly Color MarkerColor = Color.FromArgb(255, 93, 182, 229);
        public static readonly Color MissionMarkerColor = Color.FromArgb(255, 240, 200, 80);

        public static readonly int[] ControlsToDisable =
        {
            // Character controls
            19, 22, 44, 

            // Weapon controls
            25, 37, 45, 47, 58,

            // Melee controls
            140, 141, 142, 143, 263, 264,

            // Shooting controls
            24, 257
        };

        public static readonly string[] InteriorProps =
        {
            "meth_lab_empty",
            "meth_lab_setup",
            "meth_lab_basic",
            "meth_lab_upgrade",
            "meth_lab_production",
            "meth_lab_security_high"
        };

        public static readonly Tuple<Vector3, float>[] MethBoxes =
        {
            Tuple.Create(new Vector3(1017.826f, -3197.861f, -39.8918f), -88.915f),
            Tuple.Create(new Vector3(1017.841f, -3198.758f, -39.8918f), -88.765f),
            Tuple.Create(new Vector3(1017.911f, -3199.92f, -39.8918f), -89.84f),
            Tuple.Create(new Vector3(1017.911f, -3200.814f, -39.8918f), -90.24f),
            Tuple.Create(new Vector3(1017.826f, -3197.861f, -39.2918f), -88.765f),
            Tuple.Create(new Vector3(1017.841f, -3198.758f, -39.2918f), -88.915f),
            Tuple.Create(new Vector3(1017.911f, -3199.92f, -39.2918f), -89.84f),
            Tuple.Create(new Vector3(1017.911f, -3200.814f, -39.2918f), -90.24f),
            Tuple.Create(new Vector3(1017.826f, -3197.861f, -38.6918f), -88.915f),
            Tuple.Create(new Vector3(1017.841f, -3198.758f, -38.6918f), -88.765f),
            Tuple.Create(new Vector3(1017.911f, -3199.92f, -38.6918f), -89.84f),
            Tuple.Create(new Vector3(1017.911f, -3200.814f, -38.6918f), -90.24f),
            Tuple.Create(new Vector3(1017.826f, -3197.861f, -38.0918f), -88.915f),
            Tuple.Create(new Vector3(1017.841f, -3198.758f, -38.0918f), -88.765f),
            Tuple.Create(new Vector3(1017.911f, -3199.92f, -38.0918f), -89.84f),
            Tuple.Create(new Vector3(1017.911f, -3200.814f, -38.0918f), -90.24f),
            Tuple.Create(new Vector3(1017.826f, -3197.861f, -37.4918f), -88.915f),
            Tuple.Create(new Vector3(1017.841f, -3198.758f, -37.4918f), -88.765f),
            Tuple.Create(new Vector3(1017.911f, -3199.92f, -37.4918f), -89.84f),
            Tuple.Create(new Vector3(1017.911f, -3200.814f, -37.4918f), -90.24f)
        };

        public static readonly Tuple<Vector3, float>[] SetupVehicleSpawns =
        {
            Tuple.Create(new Vector3(2924.765f, 4643.386f, 47.545f), 314.799f),
            Tuple.Create(new Vector3(2594.956f, 2413.781f, 22.4907f), 172.3998f),
            Tuple.Create(new Vector3(2636.992f, 579.6707f, 94.2817f), 12.7995f),
            Tuple.Create(new Vector3(-225.6168f, -2484.773f, 5.0014f), 89.7991f),
            Tuple.Create(new Vector3(-1128.903f, -1752.743f, 3.0021f), 307.9989f),
            Tuple.Create(new Vector3(-3110.97f, 779.9443f, 17.9071f), 215.1983f),
            Tuple.Create(new Vector3(-1487.848f, 1533.011f, 112.763f), 171.998f),
            Tuple.Create(new Vector3(-2285.361f, 4251.587f, 42.1319f), 158.198f),
            Tuple.Create(new Vector3(690.1229f, 769.2505f, 204.4732f), 295.1997f),
            Tuple.Create(new Vector3(131.4018f, -400.5652f, 40.1941f), 223.3986f),
        };

        public static readonly VehicleHash[] DeliveryVehicles =
        {
            VehicleHash.Burrito3,
            VehicleHash.Rumpo,
            VehicleHash.Speedo,
            VehicleHash.Youga
        };

        public static Vector3[] CityDeliveryPositions =
        {
            new Vector3(467.219f, -1540.298f, 28.293f),
            new Vector3(-1311.157f, -598.695f, 27.296f),
            new Vector3(-1021.564f, -1127.712f, 1.1025f),
            new Vector3(-634.427f, -1223.875f, 11.136f),
            new Vector3(-599.348f, 175.415f, 64.207f),
            new Vector3(-705.872f, -302.134f, 35.751f),
            new Vector3(3.266f, -1824.835f, 24.368f),
            new Vector3(-356.377f, 81.446f, 62.787f),
            new Vector3(455.4892f, -725.7125f, 26.3591f),
            new Vector3(-1306.995f, -168.6501f, 43.0315f),
            new Vector3(-22.5962f, 215.1706f, 105.5648f),
            new Vector3(971.0678f, -633.0916f, 56.4665f),
            new Vector3(1397.159f, -1535.653f, 56.7074f),
            new Vector3(852.9148f, -2307.143f, 29.3404f),
            new Vector3(475.8266f, -1062.647f, 28.2115f),
            new Vector3(-1966.061f, -500.5934f, 10.826f),
            new Vector3(-307.141f, -275.295f, 30.389f),
            new Vector3(315.878f, -181.709f, 56.382f),
            new Vector3(-246.67f, -774.785f, 31.459f),
            new Vector3(-109.298f, -1458.554f, 32.464f),
            new Vector3(-1017.321f, 504.8095f, 78.4535f),
            new Vector3(-1630.454f, 81.513f, 61.2486f),
            new Vector3(-1514.633f, -413.2709f, 34.597f),
            new Vector3(-1428.564f, -649.4637f, 27.6734f),
            new Vector3(-663.382f, -967.998f, 20.1988f),
            new Vector3(-572.2941f, -858.0829f, 25.2536f),
            new Vector3(-82.0467f, -1315.419f, 28.2145f),
            new Vector3(488.1603f, -1278.896f, 28.4124f),
            new Vector3(1268.753f, -1583.315f, 51.6875f),
            new Vector3(159.3053f, -1816.814f, 27.2342f),
            new Vector3(953.2253f, -1719.283f, 29.6613f),
            new Vector3(-759.6809f, 365.0873f, 86.8667f),
            new Vector3(1244.374f, -346.5426f, 68.0822f),
            new Vector3(414.6804f, -2067.89f, 20.4995f),
            new Vector3(-313.8132f, -1537.828f, 26.6973f),
            new Vector3(-1263.321f, -1373.731f, 3.1453f),
            new Vector3(-3049.436f, 173.3395f, 10.6054f),
            new Vector3(1384.91f, -2044.364f, 50.9985f),
            new Vector3(-154.4409f, 987.5869f, 233.9823f),
            new Vector3(149.4402f, -2400.073f, 5.0003f)
        };

        public static Vector3[] CountrysideDeliveryPositions =
        {
            new Vector3(1946.553f, 3848.936f, 31.173f),
            new Vector3(1841.062f, 3895.484f, 32.2833f),
            new Vector3(1560.217f, 3800.145f, 33.2544f),
            new Vector3(1588.55f, 2900.709f, 56.058f),
            new Vector3(1409.915f, 2167.996f, 96.5534f),
            new Vector3(1309.234f, 1110.166f, 104.6029f),
            new Vector3(-1138.139f, 2676.507f, 17.0939f),
            new Vector3(-255.5927f, 2185.608f, 129.4257f),
            new Vector3(163.042f, 2232.03f, 89.145f),
            new Vector3(215.6581f, 3040.218f, 41.2306f),
            new Vector3(518.198f, 3081.815f, 39.56f),
            new Vector3(1026.597f, 2654.781f, 38.5511f),
            new Vector3(470.1515f, 2613.429f, 42.1769f),
            new Vector3(2166.767f, 3359.911f, 44.514f),
            new Vector3(1764.448f, 3309.967f, 40.1595f),
            new Vector3(650.7023f, 3502.217f, 33.1276f),
            new Vector3(807.696f, 2180.311f, 51.2812f),
            new Vector3(1529.466f, 1724.381f, 109.119f),
            new Vector3(1412.718f, 3614.222f, 33.912f),
            new Vector3(-39.492f, 2857.8f, 58.224f),
            new Vector3(2727.714f, 4291.594f, 47.3187f),
            new Vector3(2523.709f, 4213.176f, 38.934f),
            new Vector3(1963.682f, 4638.882f, 39.7992f),
            new Vector3(1681.745f, 4680.968f, 42.0554f),
            new Vector3(1709.24f, 4714f, 41.3369f),
            new Vector3(1687.665f, 4971.679f, 41.773f),
            new Vector3(1905.494f, 4924.755f, 47.902f),
            new Vector3(1600.131f, 6447.809f, 24.268f),
            new Vector3(1074.053f, 6507.302f, 19.959f),
            new Vector3(406.933f, 6641.465f, 27.304f),
            new Vector3(-11.297f, 6613.899f, 30.393f),
            new Vector3(-157.479f, 6450.529f, 30.441f),
            new Vector3(-110.5607f, 6251.098f, 30.2798f),
            new Vector3(-425.5597f, 6208.72f, 29.9159f),
            new Vector3(-773.2371f, 5531.055f, 32.4779f),
            new Vector3(-841.17f, 5406.444f, 33.615f),
            new Vector3(2237.801f, 5161.438f, 58.2713f),
            new Vector3(1421.668f, 4368.633f, 43.304f),
            new Vector3(763.354f, 4176.299f, 39.58f),
            new Vector3(2540.658f, 4677.303f, 32.779f)
        };

        public static Tuple<Vector3, float>[] LabWorkerPositions =
        {
            Tuple.Create(new Vector3(1003.80f, -3199.41f, -38.99f), 160.0f),
            Tuple.Create(new Vector3(1010.73f, -3200.5f, -38.99f), 0.0f),
            Tuple.Create(new Vector3(1015.0f, -3195.05f, -38.99f), 360.0f),
        };
    }
}
