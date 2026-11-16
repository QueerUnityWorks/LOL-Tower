using UnityEngine;

namespace ElementalTactics
{
    // Matches the enlarged blue turret in MainHubArt.City without changing its appearance.
    public sealed class HomeTowerHitArea : MonoBehaviour, ICanvasRaycastFilter
    {
        public Rect tower, plaque;
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            Vector2 point;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screenPoint,eventCamera,out point)) return false;
            if(plaque.Contains(point)) return true;
            float x=(point.x-tower.xMin)/tower.width;
            float y=point.y-tower.yMin, h=tower.height;
            // Stepped base, tapered body, projecting gold bands, crown and crystal.
            if(y>=0 && y<=h*.18f && x>=.03f && x<=.97f) return true;
            if(y>=h*.16f && y<=h*.78f && Mathf.Abs(x-.5f)<=Mathf.Lerp(.31f,.16f,Mathf.InverseLerp(h*.16f,h*.78f,y))) return true;
            if(y>=h*.25f && y<=h*.25f+25.56f && x>=.16f && x<=.84f) return true;
            if(y>=h*.73f && y<=Mathf.Max(h*.99f,h*.825f+63.9f) && x>=.25f && x<=.75f) return true;
            return false;
        }
    }
}
