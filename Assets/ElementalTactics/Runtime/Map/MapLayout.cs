using System;

namespace ElementalTactics
{
    public sealed class MapLayoutPoint
    {
        public readonly int Row, Bend;
        public readonly float X, Y;
        public readonly int[] Next;
        public MapLayoutPoint(int row, float x, float y, int bend, params int[] next)
        { Row = row; X = x; Y = y; Bend = bend; Next = next; }
    }

    public static class MapLayout
    {
        public const float Spacing = 126f;
        public const float SerpentineSpacing = 180f;
        public const int EntryCount = 5;
        public const int TemplateCount = 4;
        private static readonly MapLayoutPoint[] circle = {
            new MapLayoutPoint(0, -394.346375f, -107.284252f, 1, 5),
            new MapLayoutPoint(0, -170.750717f, 298.026706f, -1, 6),
            new MapLayoutPoint(0, -5.670705f, -392.50254f, 1, 8),
            new MapLayoutPoint(0, 176.117269f, 287.22487f, 1, 7),
            new MapLayoutPoint(0, 403.477938f, -97.585603f, 1, 9),
            new MapLayoutPoint(1, -336.654648f, 4.732107f, 1, 10),
            new MapLayoutPoint(1, -124.061689f, 180.99622f, 1, 11),
            new MapLayoutPoint(1, 50.978692f, 301.933259f, 1, 11),
            new MapLayoutPoint(1, 60.574032f, -285.322156f, 1, 13),
            new MapLayoutPoint(1, 277.478505f, -97.207603f, 1, 12),
            new MapLayoutPoint(2, -402.899385f, 111.912491f, -1, 14),
            new MapLayoutPoint(2, 1.8425f, 185.908974f, 1, 16),
            new MapLayoutPoint(2, 152.212623f, -83.626042f, -1, 15),
            new MapLayoutPoint(2, 182.499607f, -317.105077f, 1, 15),
            new MapLayoutPoint(3, -277.104017f, 104.734379f, 1, 17),
            new MapLayoutPoint(3, 122.80104f, -206.14526f, 1, 18),
            new MapLayoutPoint(3, 126.746381f, 202.492717f, -1, 19),
            new MapLayoutPoint(4, -250.641083f, -18.455365f, 1, 20),
            new MapLayoutPoint(4, 61.114975f, -96.277933f, -1, 21),
            new MapLayoutPoint(4, 184.773897f, 90.649938f, 1, 22),
            new MapLayoutPoint(5, -126f, 0f, 1, 23),
            new MapLayoutPoint(5, -64.304863f, -108.355363f, 1, 23),
            new MapLayoutPoint(5, 60.363247f, 110.599631f, 1, 23),
            new MapLayoutPoint(6, 0f, 0f, 1)
        };
        private static readonly MapLayoutPoint[] square = {
            new MapLayoutPoint(0, -338.055547f, -110.164786f, 1, 6),
            new MapLayoutPoint(0, -331.648156f, 340.058955f, 1, 5),
            new MapLayoutPoint(0, 102.270474f, -331.976225f, 1, 8),
            new MapLayoutPoint(0, 331.694757f, 339.12547f, -1, 9),
            new MapLayoutPoint(0, 339.355663f, -111.552203f, 1, 7),
            new MapLayoutPoint(1, -336.56091f, 214.154765f, -1, 11),
            new MapLayoutPoint(1, -214.091241f, -132.722512f, 1, 10),
            new MapLayoutPoint(1, 214.0496f, -98.346499f, -1, 13),
            new MapLayoutPoint(1, 227.452613f, -346.309133f, 1, 13),
            new MapLayoutPoint(1, 337.362843f, 213.253023f, 1, 12),
            new MapLayoutPoint(2, -224.662799f, -7.166779f, 1, 14),
            new MapLayoutPoint(2, -212.876217f, 238.198404f, 1, 14),
            new MapLayoutPoint(2, 213.07413f, 233.948817f, 1, 16),
            new MapLayoutPoint(2, 242.161002f, -221.170557f, 1, 15),
            new MapLayoutPoint(3, -246.848531f, 116.864641f, -1, 17),
            new MapLayoutPoint(3, 116.161002f, -221.170557f, 1, 18),
            new MapLayoutPoint(3, 235.259863f, 109.917396f, -1, 19),
            new MapLayoutPoint(4, -121.428693f, 128.942071f, -1, 21),
            new MapLayoutPoint(4, -6.045678f, -251.854876f, -1, 20),
            new MapLayoutPoint(4, 251.093888f, -15.083738f, 1, 22),
            new MapLayoutPoint(5, -6.045678f, -125.854876f, 1, 23),
            new MapLayoutPoint(5, 4.53502f, 125.918361f, 1, 23),
            new MapLayoutPoint(5, 126f, 0f, 1, 23),
            new MapLayoutPoint(6, 0f, 0f, 1)
        };
        private static readonly MapLayoutPoint[] triangle = {
            new MapLayoutPoint(0, -295.69888f, -86.24965f, 1, 5),
            new MapLayoutPoint(0, -58.305716f, 292.495992f, 1, 6),
            new MapLayoutPoint(0, 61.126742f, 300.929958f, 1, 6),
            new MapLayoutPoint(0, 233.84172f, -209.639664f, 1, 7),
            new MapLayoutPoint(0, 286.162485f, -95.736915f, 1, 7),
            new MapLayoutPoint(1, -170.74581f, -102.458607f, -1, 9),
            new MapLayoutPoint(1, 9.220385f, 186.118243f, -1, 8),
            new MapLayoutPoint(1, 160.672441f, -107.061613f, 1, 10),
            new MapLayoutPoint(2, -115.683496f, 202.701985f, 1, 11),
            new MapLayoutPoint(2, -45.190077f, -113.030165f, 1, 12),
            new MapLayoutPoint(2, 113.282075f, 9.686635f, 1, 13),
            new MapLayoutPoint(3, -192.497379f, 102.823923f, 1, 14),
            new MapLayoutPoint(3, -122.00396f, -212.908227f, 1, 15),
            new MapLayoutPoint(3, 187.676993f, 111.37929f, 1, 16),
            new MapLayoutPoint(4, -251.862798f, -8.314491f, 1, 17),
            new MapLayoutPoint(4, 3.914401f, -217.443247f, 1, 19),
            new MapLayoutPoint(4, 61.686065f, 109.867326f, 1, 18),
            new MapLayoutPoint(5, -125.931399f, -4.157245f, 1, 20),
            new MapLayoutPoint(5, -64.304863f, 108.355363f, 1, 20),
            new MapLayoutPoint(5, 65.600466f, -107.575921f, 1, 20),
            new MapLayoutPoint(6, 0f, 0f, 1)
        };
        private static readonly MapLayoutPoint[] serpentine = CreateSerpentine();
        private static readonly MapLayoutPoint[][] shortFloors = { CreateShortFloor(0), CreateShortFloor(1), CreateShortFloor(2), CreateShortFloor(3) };
        public static int SelectTemplate(int seed, int floor) => new Random(unchecked(seed * 397 ^ floor * 7919)).Next(TemplateCount);
        public static MapLayoutPoint[] ForFloor(int floor, int seed) => GetTemplate(SelectTemplate(seed, floor), floor == 4 ? 4 : 7);
        public static string TemplateName(int template) => template == 0 ? "圆形回廊" : template == 1 ? "方形城塞" : template == 2 ? "三角秘阵" : "双湾迷阵";
        public static MapLayoutPoint[] GetTemplate(int template, int rows)
        {
            if (template < 0 || template >= TemplateCount) throw new ArgumentOutOfRangeException(nameof(template));
            if (rows == 4) return shortFloors[template];
            if (rows != 7) throw new ArgumentOutOfRangeException(nameof(rows));
            return template == 0 ? circle : template == 1 ? square : template == 2 ? triangle : serpentine;
        }
        private static MapLayoutPoint[] CreateSerpentine()
        {
            var points = new MapLayoutPoint[31];
            double halfLaneAngle = 10 * Math.PI / 180;
            double[] shifts = { 0, -.16, -.16 - halfLaneAngle, -.33, -.24, -.10, -.10 + halfLaneAngle };
            double spacing = SerpentineSpacing;
            for (int lane = 0; lane < EntryCount; lane++)
            {
                double angle = (50 + lane * 20) * Math.PI / 180;
                double radius = 0;
                for (int step = 1; step <= 6; step++)
                {
                    double nextAngle = (50 + lane * 20) * Math.PI / 180 + shifts[step];
                    double turn = nextAngle - angle;
                    double side = radius * Math.Sin(turn);
                    radius = radius * Math.Cos(turn) + Math.Sqrt(spacing * spacing - side * side);
                    angle = nextAngle;
                    int row = 6 - step;
                    int next = row == 5 ? 30 : (row + 1) * EntryCount + lane;
                    int bend = step >= 4 ? -1 : 1;
                    points[row * EntryCount + lane] = new MapLayoutPoint(row,
                        (float)(radius * Math.Cos(angle)), (float)(-radius * Math.Sin(angle)), bend, next);
                }
            }
            points[30] = new MapLayoutPoint(6, 0, 0, 1);
            foreach (var pair in new[] { new[] { 0, 6 }, new[] { 2, 8 }, new[] { 21, 25 }, new[] { 23, 27 } })
            {
                var point = points[pair[0]];
                points[pair[0]] = new MapLayoutPoint(point.Row, point.X, point.Y, point.Bend, point.Next[0], pair[1]);
            }
            return points;
        }
        private static MapLayoutPoint[] CreateShortFloor(int template)
        {
            var points = new MapLayoutPoint[12];
            int entrance = 0;
            for (int arm = 0; arm < 3; arm++)
            {
                double angle = (90 + arm * (template == 1 ? 90 : 120) + (template == 2 ? 60 : 0)) * Math.PI / 180;
                float horizontal = (float)Math.Cos(angle), vertical = (float)Math.Sin(angle);
                points[5 + arm] = new MapLayoutPoint(1, horizontal * Spacing * 2, vertical * Spacing * 2, 1, 8 + arm);
                points[8 + arm] = new MapLayoutPoint(2, horizontal * Spacing, vertical * Spacing, 1, 11);
                int count = arm < 2 ? 2 : 1;
                for (int branch = 0; branch < count; branch++)
                {
                    double spread = template == 2 ? .75 : .6;
                    double direction = angle + (count == 1 ? 0 : branch == 0 ? -spread : spread);
                    points[entrance++] = new MapLayoutPoint(0, horizontal * Spacing * 2 + (float)Math.Cos(direction) * Spacing,
                        vertical * Spacing * 2 + (float)Math.Sin(direction) * Spacing, branch == 0 ? -1 : 1, 5 + arm);
                }
            }
            points[11] = new MapLayoutPoint(3, 0, 0, 1);
            return points;
        }
    }
}
