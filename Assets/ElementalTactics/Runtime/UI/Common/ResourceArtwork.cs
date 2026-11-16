using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Paths are relative to Resources and omit the file extension, like event CG paths.
    public sealed class ResourceArtwork : RawImage
    {
        [SerializeField] private string imagePath;
        public string ImagePath => imagePath;
        public bool cropToFill;

        public static ResourceArtwork Create(Transform parent, string name, string path, Vector2 position, Vector2 size, bool crop = false)
        {
            var image = PrototypeUI.Rect(name, parent, position, size).gameObject.AddComponent<ResourceArtwork>();
            image.raycastTarget = false;
            image.cropToFill = crop;
            image.SetPath(path);
            return image;
        }

        public void SetPath(string path)
        {
            if (ImagePath == path && texture != null) return;
            imagePath = path;
            texture = string.IsNullOrEmpty(path) ? null : Resources.Load<Texture2D>(path);
            if (texture == null && !string.IsNullOrEmpty(path)) Debug.LogWarning("找不到图片资源 Resources/" + path, this);
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!string.IsNullOrEmpty(imagePath)) SetPath(imagePath);
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (texture == null || texture.width <= 0 || texture.height <= 0) return;
            Rect bounds = GetPixelAdjustedRect();
            if (bounds.width <= 0 || bounds.height <= 0) return;
            Rect uv = new Rect(0, 0, 1, 1);
            float sourceAspect = (float)texture.width / texture.height;
            float targetAspect = bounds.width / bounds.height;
            if (cropToFill)
            {
                if (targetAspect > sourceAspect)
                {
                    float height = sourceAspect / targetAspect;
                    uv = new Rect(0, (1 - height) / 2, 1, height);
                }
                else
                {
                    float width = targetAspect / sourceAspect;
                    uv = new Rect((1 - width) / 2, 0, width, 1);
                }
            }
            else if (targetAspect > sourceAspect)
            {
                float width = bounds.height * sourceAspect;
                bounds.x += (bounds.width - width) / 2;
                bounds.width = width;
            }
            else
            {
                float height = bounds.width / sourceAspect;
                bounds.y += (bounds.height - height) / 2;
                bounds.height = height;
            }
            helper.AddVert(new Vector3(bounds.xMin, bounds.yMin), color, new Vector2(uv.xMin, uv.yMin));
            helper.AddVert(new Vector3(bounds.xMin, bounds.yMax), color, new Vector2(uv.xMin, uv.yMax));
            helper.AddVert(new Vector3(bounds.xMax, bounds.yMax), color, new Vector2(uv.xMax, uv.yMax));
            helper.AddVert(new Vector3(bounds.xMax, bounds.yMin), color, new Vector2(uv.xMax, uv.yMin));
            helper.AddTriangle(0, 1, 2);
            helper.AddTriangle(2, 3, 0);
        }
    }
}
