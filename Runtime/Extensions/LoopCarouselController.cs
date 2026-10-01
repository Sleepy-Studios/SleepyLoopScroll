using System;
using UnityEngine;

namespace SleepyStudios.LoopScroll
{
    [DisallowMultipleComponent, RequireComponent(typeof(LoopScrollView))]
    [DefaultExecutionOrder(110)]
    public sealed class LoopCarouselController : MonoBehaviour
    {
        [SerializeField, Min(1)] private float pageSize = 300;
        [SerializeField, Min(0)] private float snapDuration = .2f;
        [SerializeField, Min(0)] private float autoPlayInterval;
        private LoopScrollView list;
        private float autoPlayElapsed;
        private bool needsSnap;
        private bool settlePending;
        public int CurrentPage { get; private set; } = -1;
        public event Action<int> PageChanged;
        public float PageSize => pageSize;
        private void Awake() { list = GetComponent<LoopScrollView>(); }
        private void OnEnable() { list.DataChanged += OnData; list.DragStarted += OnDragStart; list.DragEnded += OnDragEnd; if (list.Count > 0) OnData(); }
        private void OnDisable()
        {
            list.DataChanged -= OnData; list.DragStarted -= OnDragStart; list.DragEnded -= OnDragEnd;
            list.CancelAnimation(); list.DisableLoop(); list.ScrollRect.StopMovement();
            list.ScrollRect.vertical = list.IsVertical; list.ScrollRect.horizontal = !list.IsVertical;
        }
        /// <summary>配置固定页尺寸与自动播放；0 秒关闭自动播放。</summary>
        /// <param name="size">主轴页尺寸。</param>
        /// <param name="interval">自动播放间隔，unscaled 秒。</param>
        /// <param name="duration">吸附动画秒数。</param>
        public void Configure(float size, float interval = 0, float duration = .2f)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            pageSize = size; autoPlayInterval = Mathf.Max(0, interval); snapDuration = Mathf.Max(0, duration);
            if (list.Count > 0) OnData();
        }
        private void OnData()
        {
            autoPlayElapsed = 0; needsSnap = false; settlePending = false;
            if (list.Count < 2)
            {
                list.ScrollRect.StopMovement();
                list.DisableLoop(); CurrentPage = list.Count == 0 ? -1 : 0;
                list.ScrollRect.horizontal = false; list.ScrollRect.vertical = false;
                if (list.Count == 1) list.ScrollToCell(0, ScrollAlignment.Center);
                PageChanged?.Invoke(CurrentPage); return;
            }
            list.EnableLoop(pageSize);
            list.ScrollRect.vertical = list.IsVertical; list.ScrollRect.horizontal = !list.IsVertical;
            SetPage(Mathf.Clamp(CurrentPage, 0, list.Count - 1), ScrollAnimation.Immediate);
        }
        private void OnDragStart() { autoPlayElapsed = 0; needsSnap = false; settlePending = false; }
        private void OnDragEnd() { needsSnap = true; }
        private int NearestVirtualPage => Mathf.RoundToInt((list.Offset + (list.ViewportLength - pageSize) * .5f) / pageSize);
        private void LateUpdate()
        {
            if (list.Count < 2 || list.IsDragging || list.ViewportLength <= 0) return;
            if (needsSnap && !list.IsAnimating && list.ScrollRect.velocity.sqrMagnitude < 400)
            { needsSnap = false; Snap(NearestVirtualPage, new ScrollAnimation(snapDuration)); }
            if (settlePending && !list.IsAnimating)
            {
                settlePending = false;
                var virtualPage = NearestVirtualPage;
                list.RecenterLoop(virtualPage, pageSize);
                var page = LoopScrollView.Mod(virtualPage, list.Count);
                if (CurrentPage != page) { CurrentPage = page; PageChanged?.Invoke(page); }
            }
            if (needsSnap || settlePending || list.IsAnimating || list.ScrollRect.velocity.sqrMagnitude > 1 || autoPlayInterval <= 0) return;
            autoPlayElapsed += Time.unscaledDeltaTime;
            if (autoPlayElapsed >= autoPlayInterval) { autoPlayElapsed = 0; Snap(NearestVirtualPage + 1, new ScrollAnimation(snapDuration)); }
        }
        private void Snap(int virtualPage, ScrollAnimation animation)
        {
            list.ScrollToOffset(virtualPage * pageSize - (list.ViewportLength - pageSize) * .5f, animation);
            settlePending = true;
        }
        /// <summary>选择真实页码；使用当前位置最近的同页循环副本。</summary>
        /// <param name="page">0 到 Count-1。</param>
        /// <param name="animation">默认立即定位。</param>
        public void SetPage(int page, ScrollAnimation animation = default)
        {
            if (page < 0 || page >= list.Count) throw new ArgumentOutOfRangeException(nameof(page));
            if (list.Count == 1) { list.ScrollToCell(0, ScrollAlignment.Center, animation); return; }
            var nearest = NearestVirtualPage;
            var cycle = Mathf.RoundToInt((nearest - page) / (float)list.Count);
            Snap(cycle * list.Count + page, animation);
        }
        /// 下一页；少于两项时无操作。
        public void Next() { if (list.Count > 1) Snap(NearestVirtualPage + 1, new ScrollAnimation(snapDuration)); }
        /// 上一页；少于两项时无操作。
        public void Previous() { if (list.Count > 1) Snap(NearestVirtualPage - 1, new ScrollAnimation(snapDuration)); }
    }
}
