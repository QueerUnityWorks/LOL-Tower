using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapCircle : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = rectTransform.rect;
            Vector2 center = bounds.center;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            mesh.AddVert(center, color, Vector2.zero);
            for (int segment = 0; segment <= 64; segment++)
            {
                float angle = segment * Mathf.PI * 2 / 64;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (segment > 0) mesh.AddTriangle(0, segment, segment + 1);
            }
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera)) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 point);
            return Vector2.Distance(point, rectTransform.rect.center) <= rectTransform.rect.width * 0.5f;
        }
    }
}
