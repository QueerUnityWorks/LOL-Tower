using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapNodeIcon : MaskableGraphic
    {
        [SerializeField] private NodeKind kind;
        public NodeKind Kind
        {
            get => kind;
            set { if (kind == value) return; kind = value; SetVerticesDirty(); }
        }
        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (kind == NodeKind.Battle)
            {
                Sword(mesh, false);
                Sword(mesh, true);
            }
            else if (kind == NodeKind.Shop)
            {
                Polygon(mesh, new Vector2(-.43f, .10f), new Vector2(-.28f, .40f), new Vector2(.28f, .40f), new Vector2(.43f, .10f));
                Bar(mesh, -.35f, -.36f, -.27f, .08f);
                Bar(mesh, .27f, -.36f, .35f, .08f);
                Bar(mesh, -.40f, -.42f, .40f, -.32f);
                Bar(mesh, -.20f, -.23f, .20f, -.15f);
            }
            else
            {
                Vector2 previous = new Vector2(-.19f, .17f);
                for (int segment = 1; segment <= 16; segment++)
                {
                    float angle = (180 - segment * 240f / 16) * Mathf.Deg2Rad;
                    Vector2 next = new Vector2(Mathf.Cos(angle) * .19f, .17f + Mathf.Sin(angle) * .19f);
                    Stroke(mesh, previous, next, .075f);
                    previous = next;
                }
                Stroke(mesh, previous, new Vector2(0, -.09f), .075f);
                Stroke(mesh, new Vector2(0, -.09f), new Vector2(0, -.18f), .075f);
                Bar(mesh, -.043f, -.34f, .043f, -.254f);
            }
        }
        private void Stroke(VertexHelper mesh, Vector2 from, Vector2 to, float width)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(direction.y, -direction.x) * (width * .5f);
            Polygon(mesh, from - side, to - side, to + side, from + side);
        }
        private void Sword(VertexHelper mesh, bool mirror)
        {
            Vector2 axis = new Vector2(mirror ? -.707f : .707f, .707f);
            Vector2 side = new Vector2(axis.y, -axis.x);
            Polygon(mesh, axis * -.12f - side * .055f, axis * .29f - side * .055f,
                axis * .43f, axis * .29f + side * .055f, axis * -.12f + side * .055f);
            Polygon(mesh, axis * -.14f - side * .18f, axis * -.07f - side * .18f,
                axis * -.07f + side * .18f, axis * -.14f + side * .18f);
            Polygon(mesh, axis * -.37f - side * .04f, axis * -.14f - side * .04f,
                axis * -.14f + side * .04f, axis * -.37f + side * .04f);
        }
        private void Bar(VertexHelper mesh, float left, float bottom, float right, float top)
        {
            Polygon(mesh, new Vector2(left, bottom), new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom));
        }
        private void Polygon(VertexHelper mesh, params Vector2[] points)
        {
            int start = mesh.currentVertCount;
            Rect bounds = rectTransform.rect;
            foreach (var point in points) mesh.AddVert(new Vector3(bounds.center.x + point.x * bounds.width, bounds.center.y + point.y * bounds.height), color, Vector2.zero);
            for (int index = 1; index < points.Length - 1; index++) mesh.AddTriangle(start, start + index, start + index + 1);
        }
    }
}
