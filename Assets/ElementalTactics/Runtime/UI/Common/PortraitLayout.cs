using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [ExecuteAlways]
    public class PortraitLayout : MonoBehaviour
    {
        public RectTransform safeArea;
        public RectTransform content;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            // Existing scenes gain the adapter automatically; scene references stay intact.
            if (GetComponent<MobileResolutionController>() == null)
                gameObject.AddComponent<MobileResolutionController>();
            foreach (var label in GetComponentsInChildren<Text>(true))
                if (label.rectTransform.rect.height < label.fontSize * 1.6f)
                    label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) return;
            var adapter = GetComponent<MobileResolutionController>();
            if (adapter != null && adapter.enabled) adapter.Refresh();
            else MobileResolutionController.ApplyLayout(safeArea, content, new Vector2(900, 1600),
                new Vector2Int(Screen.width, Screen.height), Screen.safeArea);
        }
    }
}
