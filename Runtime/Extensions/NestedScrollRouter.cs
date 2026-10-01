using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SleepyStudios.LoopScroll
{
    // 挂在子 ScrollRect 的 Viewport 上，使 EventSystem 首先选择路由器作为 dragHandler。
    [DisallowMultipleComponent]
    public sealed class NestedScrollRouter : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ScrollRect child;
        [SerializeField] private ScrollRect parent;
        private ScrollRect target;
        private bool handedOff;
        private PointerEventData currentDrag;
        /// <summary>配置父子滚动；必须挂在子 Viewport 或其射线命中的子节点。</summary>
        /// <param name="childScroll">子 ScrollRect。</param>
        /// <param name="parentScroll">父 ScrollRect。</param>
        public void Configure(ScrollRect childScroll, ScrollRect parentScroll) { child = childScroll; parent = parentScroll; }
        public void OnInitializePotentialDrag(PointerEventData eventData) { child?.OnInitializePotentialDrag(eventData); parent?.OnInitializePotentialDrag(eventData); }
        public void OnBeginDrag(PointerEventData eventData)
        {
            handedOff = false;
            currentDrag = eventData;
            var vertical = Mathf.Abs(eventData.delta.y) >= Mathf.Abs(eventData.delta.x);
            target = parent != null && child != null && !(vertical ? child.vertical : child.horizontal) ? parent : child;
            target?.OnBeginDrag(eventData); NotifyBegin(target, eventData);
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (!handedOff && target == child && parent != null && AtBoundary(eventData.delta))
            {
                child.OnEndDrag(eventData); NotifyEnd(child, eventData);
                target = parent; handedOff = true;
                // 父滚动从本次当前位置建基线，后续 delta 不会重复应用整个手势位移。
                parent.OnInitializePotentialDrag(eventData); parent.OnBeginDrag(eventData); NotifyBegin(parent, eventData);
            }
            target?.OnDrag(eventData);
        }
        public void OnEndDrag(PointerEventData eventData) { target?.OnEndDrag(eventData); NotifyEnd(target, eventData); target = null; currentDrag = null; }
        private void OnDisable() { if (currentDrag != null) OnEndDrag(currentDrag); }
        private bool AtBoundary(Vector2 delta)
        {
            if (child == null || child.content == null || child.viewport == null || child.movementType == ScrollRect.MovementType.Unrestricted) return false;
            if (child.vertical)
            {
                if (child.content.rect.height <= child.viewport.rect.height) return true;
                var position = child.verticalNormalizedPosition;
                return (position >= .999f && delta.y < 0) || (position <= .001f && delta.y > 0);
            }
            if (child.content.rect.width <= child.viewport.rect.width) return true;
            return (child.horizontalNormalizedPosition <= .001f && delta.x > 0) || (child.horizontalNormalizedPosition >= .999f && delta.x < 0);
        }
        private static void NotifyBegin(ScrollRect rect, PointerEventData data) { var loop = rect != null ? rect.GetComponent<LoopScrollView>() : null; if (loop != null) loop.OnBeginDrag(data); }
        private static void NotifyEnd(ScrollRect rect, PointerEventData data) { var loop = rect != null ? rect.GetComponent<LoopScrollView>() : null; if (loop != null) loop.OnEndDrag(data); }
    }
}
