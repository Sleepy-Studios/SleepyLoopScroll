using System;
using UnityEngine;

namespace SleepyStudios.LoopScroll
{
    [DisallowMultipleComponent, RequireComponent(typeof(LoopScrollView))]
    public sealed class LoopPagingTrigger : MonoBehaviour
    {
        [SerializeField] private bool startEnabled;
        [SerializeField] private bool endEnabled = true;
        [SerializeField, Min(0)] private float thresholdViewports = 1;
        private LoopScrollView list;
        private bool pending;
        public PagingState StartState { get; private set; }
        public PagingState EndState { get; private set; }
        public event Action<PagingBoundary> LoadRequested;
        private void Awake() { list = GetComponent<LoopScrollView>(); }
        private void OnEnable() { list.ScrollPositionChanged += MarkPending; list.DataChanged += MarkPending; pending = true; }
        private void OnDisable() { list.ScrollPositionChanged -= MarkPending; list.DataChanged -= MarkPending; }
        private void MarkPending() { pending = true; }
        private void LateUpdate() { if (pending) { pending = false; Evaluate(); } }
        /// <summary>配置两个边界是否启用；不重置已有加载状态。</summary>
        /// <param name="start">是否加载前方数据。</param>
        /// <param name="end">是否加载后方数据。</param>
        /// <param name="viewports">触发距离，相对于 Viewport 长度。</param>
        public void Configure(bool start, bool end, float viewports = 1)
        { startEnabled = start; endEnabled = end; thresholdViewports = Mathf.Max(0, viewports); pending = true; }
        /// 检查距离；先进入 Loading 再抛事件，防止同步回包与重复触发重入。
        public void Evaluate()
        {
            if (!isActiveAndEnabled || list.IsLooping || list.ViewportLength <= 0 || LoadRequested == null) return;
            var threshold = thresholdViewports * list.ViewportLength;
            if (startEnabled && StartState == PagingState.Idle && list.DistanceToStart <= threshold) Request(PagingBoundary.Start);
            if (endEnabled && EndState == PagingState.Idle && list.DistanceToEnd <= threshold) Request(PagingBoundary.End);
        }
        private void Request(PagingBoundary boundary)
        {
            SetState(boundary, PagingState.Loading);
            try { LoadRequested?.Invoke(boundary); }
            catch { SetState(boundary, PagingState.Error); throw; }
        }
        /// <summary>宿主更新数据后结束请求；无更多数据时进入 Completed。</summary>
        /// <param name="boundary">本次请求边界。</param>
        /// <param name="hasMore">是否还有下一批数据。</param>
        public void Complete(PagingBoundary boundary, bool hasMore)
        { SetState(boundary, hasMore ? PagingState.Idle : PagingState.Completed); pending = hasMore; }
        /// <summary>宿主报告加载错误；不会自动重复请求。</summary>
        /// <param name="boundary">失败边界。</param>
        public void Fail(PagingBoundary boundary) { SetState(boundary, PagingState.Error); }
        /// <summary>显式重试失败请求，忽略非 Error 状态。</summary>
        /// <param name="boundary">待重试边界。</param>
        public void Retry(PagingBoundary boundary)
        { if (GetState(boundary) == PagingState.Error && LoadRequested != null && isActiveAndEnabled) Request(boundary); }
        /// <summary>更换查询条件后重置状态；旧网络请求的取消由宿主负责。</summary>
        /// <param name="boundary">重置边界。</param>
        public void ResetBoundary(PagingBoundary boundary) { SetState(boundary, PagingState.Idle); pending = true; }
        public PagingState GetState(PagingBoundary boundary) => boundary == PagingBoundary.Start ? StartState : EndState;
        private void SetState(PagingBoundary boundary, PagingState state)
        { if (boundary == PagingBoundary.Start) StartState = state; else EndState = state; }
    }
}
