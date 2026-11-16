using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoleEmblem : MaskableGraphic
    {
        public Role role;

        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            switch (role)
            {
                case Role.Warrior:
                    Polygon(helper, new Vector2(-0.10f, -0.20f), new Vector2(-0.10f, 0.28f), new Vector2(0, 0.46f), new Vector2(0.10f, 0.28f), new Vector2(0.10f, -0.20f));
                    Bar(helper, -0.28f, -0.23f, 0.28f, -0.16f);
                    Bar(helper, -0.055f, -0.43f, 0.055f, -0.23f);
                    break;
                case Role.Mage:
                    Polygon(helper, new Vector2(0, 0.46f), new Vector2(0.22f, 0.16f), new Vector2(0, -0.1f), new Vector2(-0.22f, 0.16f));
                    Bar(helper, -0.035f, -0.44f, 0.035f, -0.08f);
                    Bar(helper, -0.18f, -0.31f, 0.18f, -0.26f);
                    break;
                case Role.Guardian:
                    Polygon(helper, new Vector2(-0.32f, 0.35f), new Vector2(0.32f, 0.35f), new Vector2(0.26f, -0.12f), new Vector2(0, -0.44f), new Vector2(-0.26f, -0.12f));
                    break;
                case Role.Ranger:
                    Polygon(helper, new Vector2(-0.20f, 0.42f), new Vector2(0.15f, 0.18f), new Vector2(0.24f, 0), new Vector2(0.15f, -0.18f), new Vector2(-0.20f, -0.42f), new Vector2(0.10f, 0));
                    Bar(helper, -0.22f, -0.025f, 0.36f, 0.025f);
                    Polygon(helper, new Vector2(0.45f, 0), new Vector2(0.29f, 0.12f), new Vector2(0.29f, -0.12f));
                    break;
            }
        }

        private void Bar(VertexHelper helper, float left, float bottom, float right, float top)
        {
            Polygon(helper, new Vector2(left, bottom), new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom));
        }

        private void Polygon(VertexHelper helper, params Vector2[] points)
        {
            int start = helper.currentVertCount;
            Rect bounds = rectTransform.rect;
            foreach (Vector2 point in points)
                helper.AddVert(new Vector3(bounds.center.x + point.x * bounds.width, bounds.center.y + point.y * bounds.height), color, Vector2.zero);
            for (int index = 1; index < points.Length - 1; index++) helper.AddTriangle(start, start + index, start + index + 1);
        }
    }
}
