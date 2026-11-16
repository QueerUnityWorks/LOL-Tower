using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapRouteGraphic : MaskableGraphic
    {
        public Vector2 From, To;
        public float Bend = 1, Thickness = 4;
        public float ArrowScale = 1;
        public bool FromBoss, ToBoss, UseVisibleArrow;
        public Vector2 Point(float fraction)
        {
            if (fraction <= 0) return From;
            if (fraction >= 1) return To;
            Vector2 direction = To - From;
            Vector2 normal = new Vector2(direction.y, -direction.x) * (.38f * Bend);
            return Vector2.Lerp(From, To, fraction) + normal * (3 * fraction * (1 - fraction));
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            int segments = UseVisibleArrow ? MapRouteGeometry.Segments : 40;
            for (int segment = 0; segment < segments; segment++)
            {
                Vector2 start = Point(segment / (float)segments), end = Point((segment + 1) / (float)segments);
                Vector2 direction = (end - start).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x) * (Thickness / 2);
                int index = mesh.currentVertCount;
                mesh.AddVert(start - normal, color, Vector2.zero);
                mesh.AddVert(start + normal, color, Vector2.zero);
                mesh.AddVert(end + normal, color, Vector2.zero);
                mesh.AddVert(end - normal, color, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index + 2, index + 3, index);
            }
            float fraction = ToBoss ? .36f : .52f;
            Vector2 tip = Point(fraction), tangent = (tip - Point(fraction - .04f)).normalized;
            if (UseVisibleArrow)
            {
                var visible = MapRouteGeometry.VisiblePoints(From, To, Bend, FromBoss, ToBoss, Thickness / 2);
                if (visible.Length < 2) return;
                int middle = System.Math.Max(1, visible.Length / 2);
                tip = visible[middle]; tangent = (tip - visible[middle - 1]).normalized;
            }
            float length = UseVisibleArrow ? 4 * ArrowScale : 6;
            Vector2 side = new Vector2(-tangent.y, tangent.x) * (UseVisibleArrow ? 3.2f * ArrowScale : 5);
            int arrow = mesh.currentVertCount;
            mesh.AddVert(tip + tangent * length, color, Vector2.zero);
            mesh.AddVert(tip - tangent * length + side, color, Vector2.zero);
            mesh.AddVert(tip - tangent * length - side, color, Vector2.zero);
            mesh.AddTriangle(arrow, arrow + 1, arrow + 2);
        }
    }
}
