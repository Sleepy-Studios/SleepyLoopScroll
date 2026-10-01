using System;
using System.Collections;
using System.Collections.Generic;
using SleepyStudios.LoopScroll.Internal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SleepyStudios.LoopScroll
{
    [DisallowMultipleComponent, RequireComponent(typeof(ScrollRect))]
    [DefaultExecutionOrder(100)]
    public sealed class LoopScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private LoopLayout layout;
        [SerializeField] private Vector2 cellSize = new Vector2(100, 48);
        [SerializeField] private Vector2 spacing;
        [SerializeField] private RectOffset padding = new RectOffset();
        [SerializeField, Min(0)] private float overscan = 96;
        [SerializeField] private bool dynamicSize;
        [SerializeField] private List<LoopCellPrefab> cellPrefabs = new List<LoopCellPrefab>();

        private sealed class Pool
        {
            public LoopCell Prefab;
            public readonly Stack<LoopCell> Free = new Stack<LoopCell>();
        }
        private readonly Dictionary<int, Pool> pools = new Dictionary<int, Pool>();
        private readonly Dictionary<int, LoopCell> active = new Dictionary<int, LoopCell>();
        private readonly List<int> recycleSlots = new List<int>(64);
        private readonly HashSet<int> invalidSizes = new HashSet<int>();
        private readonly Dictionary<string, float> measuredSizes = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly FenwickTree sizes = new FenwickTree();
        private Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);
        private string[] keys = Array.Empty<string>();
        private int[] types = Array.Empty<int>();
        private ILoopDataSource source;
        private Action<LoopCell, int, CellBindContext> registeredBind;
        private Action<LoopCell, CellBindContext> registeredUnbind;
        private RectTransform poolRoot;
        private Vector2 viewportSize;
        private bool initialized;
        private bool dirty = true;
        private bool reconciling;
        private bool committing;
        private bool handlingCellCallbacks;
        private PointerEventData currentDrag;
        private bool refreshPending;
        private bool looping;
        private float loopPageSize;
        private float animationFrom;
        private float animationTo;
        private float animationTime;
        private float animationDuration;
        private Action<LoopCell, CellBindContext> cachedClick;
        private RefillOptions? pendingOptions;
        private ScrollRect.MovementType savedMovement;
        private Scrollbar savedHorizontalBar;
        private Scrollbar savedVerticalBar;
        private string alignedKey;
        private ScrollAlignment alignedAlignment;
        private bool followEndDuringMeasurement;
        // 请求保存在值字段中；无回调定位不创建请求对象或逐次闭包。
        private bool hasScrollRequest;
        private bool scrollPending;
        private int scrollIndex;
        private ScrollAlignment scrollAlignment;
        private float scrollOffsetPixels;
        private float scrollOffset;
        private float scrollDuration;
        private int scrollStartedFrame;
        private Action<ScrollResult> scrollFinished;
        private int scrollOperationDepth;
        private bool notifyingScroll;
        private bool destroying;
        private readonly List<ScrollNotification> scrollNotifications = new List<ScrollNotification>(4);
        private struct ScrollNotification
        {
            public Action<ScrollResult> Callback;
            public ScrollResult Result;
        }

        public event Action<LoopCell, CellBindContext> CellBound;
        public event Action<LoopCell, CellBindContext> CellUnbound;
        public event Action<LoopCell, CellBindContext> CellClicked;
        public event Action DataChanged;
        public event Action ScrollPositionChanged;
        public event Action DragStarted;
        public event Action DragEnded;
        public ScrollRect ScrollRect => scrollRect != null ? scrollRect : (scrollRect = GetComponent<ScrollRect>());
        public bool IsVertical => layout == LoopLayout.Vertical || layout == LoopLayout.VerticalGrid;
        public bool IsGrid => layout == LoopLayout.VerticalGrid || layout == LoopLayout.HorizontalGrid;
        public bool HasStableKeys => source != null && source.HasStableKeys;
        public bool IsDragging { get; private set; }
        /// 仅表示插值动画正在运行；等待执行和布局收敛通过定位回调判断结束。
        public bool IsAnimating => animationDuration > 0;
        public bool IsLooping => looping;
        public int Count => keys.Length;
        public int ActiveCellCount => active.Count;
        public int CreatedCellCount { get; private set; }
        public VisibleRange VisibleRange { get; private set; } = VisibleRange.Empty;
        public float ViewportLength => IsVertical ? viewportSize.y : viewportSize.x;
        public float ContentLength => IsGrid ? StartPadding + EndPadding + Bands * Stride - (Bands > 0 ? AxisSpacing : 0)
            : StartPadding + EndPadding + sizes.Total - (Count > 0 ? AxisSpacing : 0);
        public float MaxOffset => Mathf.Max(0, ContentLength - ViewportLength);
        public float Offset => ScrollRect.content == null ? 0 : IsVertical ? ScrollRect.content.anchoredPosition.y : -ScrollRect.content.anchoredPosition.x;
        public float DistanceToStart => Mathf.Max(0, Offset);
        public float DistanceToEnd => Mathf.Max(0, MaxOffset - Offset);
        public float DefaultItemSize => IsVertical ? cellSize.y : cellSize.x;
        private float CrossSize => IsVertical ? viewportSize.x : viewportSize.y;
        private float AxisSpacing => IsVertical ? spacing.y : spacing.x;
        private float CrossSpacing => IsVertical ? spacing.x : spacing.y;
        private float CrossCellSize => IsVertical ? cellSize.x : cellSize.y;
        private float StartPadding => IsVertical ? padding.top : padding.left;
        private float EndPadding => IsVertical ? padding.bottom : padding.right;
        private float CrossStartPadding => IsVertical ? padding.left : padding.top;
        private float CrossEndPadding => IsVertical ? padding.right : padding.bottom;
        private float Stride => DefaultItemSize + AxisSpacing;
        private int Lanes => Mathf.Max(1, Mathf.FloorToInt((CrossSize - CrossStartPadding - CrossEndPadding + CrossSpacing) / (CrossCellSize + CrossSpacing)));
        private int Bands => (Count + Lanes - 1) / Lanes;

        /// <summary>初始化层级及 Prefab 映射；只能在首次提交数据前调用。</summary>
        /// <param name="rect">具有 Viewport、Content 的原生 ScrollRect。</param>
        /// <param name="prefabs">每个类型唯一的 LoopCell Prefab。</param>
        /// <param name="mode">单轴 List 或规则 Grid。</param>
        /// <param name="size">固定 Cell 尺寸；动态 List 以主轴分量作为估算值。</param>
        /// <param name="measureDynamic">是否测量 List 的主轴尺寸；Grid 不支持。</param>
        public void Configure(ScrollRect rect, IReadOnlyList<LoopCellPrefab> prefabs, LoopLayout mode, Vector2 size, bool measureDynamic = false)
        {
            if (initialized) throw new InvalidOperationException("Configure 必须在首次提交数据前调用。");
            scrollRect = rect; layout = mode; cellSize = size; dynamicSize = measureDynamic;
            cellPrefabs.Clear();
            for (var i = 0; i < prefabs.Count; i++) cellPrefabs.Add(prefabs[i]);
            ValidateConfiguration();
        }

        /// <summary>检查必需引用、尺寸、布局冲突和类型映射；不改变场景。</summary>
        public void ValidateConfiguration()
        {
            var rect = ScrollRect;
            if (rect.content == null || rect.viewport == null) throw new InvalidOperationException("LoopScrollView 需要显式 Viewport 和 Content。");
            if (rect.content.GetComponent<LayoutGroup>() != null || rect.content.GetComponent<ContentSizeFitter>() != null)
                throw new InvalidOperationException("Content 不能挂 LayoutGroup 或 ContentSizeFitter；布局组件请放在 Cell 内部。");
            if (cellSize.x <= 0 || cellSize.y <= 0 || spacing.x < 0 || spacing.y < 0 || !Finite(cellSize.x) || !Finite(cellSize.y))
                throw new InvalidOperationException("Cell 尺寸必须为有限正数，Spacing 不能为负。");
            if (dynamicSize && IsGrid) throw new InvalidOperationException("v0.1 Grid 只支持固定 Cell 尺寸。");
            var seen = new HashSet<int>();
            for (var i = 0; i < cellPrefabs.Count; i++)
            {
                var entry = cellPrefabs[i];
                if (entry == null || entry.Prefab == null || !seen.Add(entry.Type))
                    throw new InvalidOperationException("Cell 类型重复或 Prefab 缺失。");
            }
            if (seen.Count == 0) throw new InvalidOperationException("至少配置一个 Cell Prefab。");
        }

        private void Initialize()
        {
            if (initialized) return;
            ValidateConfiguration();
            cachedClick = HandleClick;
            poolRoot = new GameObject("LoopCellPool", typeof(RectTransform)).GetComponent<RectTransform>();
            poolRoot.SetParent(transform, false);
            poolRoot.gameObject.SetActive(false);
            for (var i = 0; i < cellPrefabs.Count; i++)
            {
                var entry = cellPrefabs[i];
                var pool = new Pool { Prefab = entry.Prefab };
                pools.Add(entry.Type, pool);
                for (var j = 0; j < entry.Prewarm; j++) pool.Free.Push(CreateCell(entry.Type, pool));
            }
            ScrollRect.content.anchorMin = ScrollRect.content.anchorMax = new Vector2(0, 1);
            ScrollRect.content.pivot = new Vector2(0, 1);
            ScrollRect.vertical = IsVertical;
            ScrollRect.horizontal = !IsVertical;
            viewportSize = ScrollRect.viewport.rect.size;
            ScrollRect.onValueChanged.AddListener(OnScroll);
            initialized = true;
        }

        /// <summary>配置一次索引式绑定，随后用 SetTotalCount 提交真实集合。旧绑定仍使用原来的解绑回调。</summary>
        /// <typeparam name="TCell">类型 0 模板上的组件。</typeparam>
        /// <param name="bind">业务通过索引读取自己拥有的数据；异步写入先检查 context.IsCurrent。</param>
        /// <param name="unbind">回收时的清理回调；此时上下文已失效。</param>
        public void RegisterCellBinding<TCell>(Action<TCell, int, CellBindContext> bind,
            Action<TCell, CellBindContext> unbind = null) where TCell : Component
        {
            if (bind == null) throw new ArgumentNullException(nameof(bind));
            if (reconciling || committing || handlingCellCallbacks)
                throw new InvalidOperationException("Cell 生命周期回调内不能重新注册绑定。");
            registeredBind = (cell, index, context) =>
            {
                var component = cell.GetComponent<TCell>();
                if (component == null) throw new InvalidOperationException($"Cell Prefab 缺少 {typeof(TCell).FullName}");
                bind(component, index, context);
            };
            registeredUnbind = unbind == null ? null : (cell, context) => unbind(cell.GetComponent<TCell>(), context);
        }

        /// <summary>提交实际集合并完整重载；null 清空。不会根据集合引用相同而跳过重载。</summary>
        /// <param name="items">调用方维护的 IList；List&lt;T&gt; 和数组可直接提交。</param>
        /// <param name="options">默认起点；保持位置或定位指定索引需显式配置。</param>
        /// <param name="getItemKey">可选稳定业务 Key。选择与保持业务锚点需要此参数。</param>
        public void SetTotalCount(IList items, RefillOptions options = default, Func<object, string> getItemKey = null)
        {
            if (registeredBind == null) throw new InvalidOperationException("请先 RegisterCellBinding，宿主 ItemView 请先注册或配置工厂。");
            SetDataSource(new ListDataSource(items ?? Array.Empty<object>(), registeredBind,
                registeredUnbind, getItemKey, DefaultItemSize), options);
        }

        /// <summary>提交高级数据源并完整重载；可在 inactive 时调用。</summary>
        /// <param name="dataSource">数据源；其 Count、Key、类型和尺寸必须一致有效。</param>
        /// <param name="options">锚点及定位策略。</param>
        public void SetDataSource(ILoopDataSource dataSource, RefillOptions options = default)
        {
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            Initialize();
            Commit(dataSource, options, true);
        }

        /// <summary>重新读取整个数据源并重新绑定；默认回到起点。</summary>
        /// <param name="options">锚点与可选目标索引。</param>
        public void RefillCells(RefillOptions options = default)
        { if (source != null) Commit(source, options, false); }

        /// <summary>重新绑定可见项，不改变数量；inactive 时延后到启用。</summary>
        public void RefreshCells()
        {
            if (reconciling || committing || handlingCellCallbacks) throw new InvalidOperationException("Cell 生命周期回调中不能重入刷新。");
            refreshPending = true; dirty = true; if (isActiveAndEnabled) Reconcile();
        }

        /// <summary>调用方完成数据修改后提交有序变更，一次刷新。错误批次不改变展示。</summary>
        /// <param name="changes">按序执行的结构描述，Move 目标是移除后的插入位置。</param>
        /// <param name="anchorPolicy">默认保持首个可见 Key 和像素偏移。</param>
        public void ApplyChanges(IReadOnlyList<LoopListChange> changes, ScrollAnchorPolicy anchorPolicy = ScrollAnchorPolicy.KeepFirstVisible)
        {
            if (source == null || changes == null) throw new ArgumentNullException(source == null ? nameof(source) : nameof(changes));
            var count = Count;
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                if (change.Count <= 0 || change.Index < 0 || change.Index > count ||
                    (change.Kind != LoopListChangeKind.Insert && change.Count > count - change.Index))
                    throw new ArgumentOutOfRangeException(nameof(changes), "变更索引或数量越界。");
                switch (change.Kind)
                {
                    case LoopListChangeKind.Insert: count = checked(count + change.Count); break;
                    case LoopListChangeKind.Remove: count -= change.Count; break;
                    case LoopListChangeKind.Replace: break;
                    case LoopListChangeKind.Move:
                        if (change.Destination < 0 || change.Destination > count - change.Count)
                            throw new ArgumentOutOfRangeException(nameof(changes), "Move 目标越界。");
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(changes));
                }
            }
            if (count != source.Count) throw new InvalidOperationException("变更批次的最终 Count 与数据源不一致。");
            Commit(source, new RefillOptions(anchorPolicy), false);
        }

        /// <summary>通知已添加到集合末尾的项。</summary>
        /// <param name="count">追加数量。</param>
        /// <param name="policy">追加后的锚点策略。</param>
        public void Append(int count, ScrollAnchorPolicy policy = ScrollAnchorPolicy.KeepFirstVisible)
        { ApplyChanges(new[] { LoopListChange.Insert(Count, count) }, policy); }
        /// <summary>通知已添加到集合开头的项。</summary>
        /// <param name="count">前插数量。</param>
        /// <param name="policy">默认保持首个可见项。</param>
        public void Prepend(int count, ScrollAnchorPolicy policy = ScrollAnchorPolicy.KeepFirstVisible)
        { ApplyChanges(new[] { LoopListChange.Insert(0, count) }, policy); }

        private struct Anchor
        {
            public string Key;
            public int Index;
            public float Within;
            public float Position;
            public string[] OldKeys;
        }
        private Anchor CaptureAnchor()
        {
            var index = Count > 0 ? IndexAt(Mathf.Max(0, Offset)) : -1;
            return new Anchor { Key = index >= 0 ? keys[index] : null, Index = index, Position = Offset,
                Within = index >= 0 ? Offset - ItemStart(index) : 0, OldKeys = keys };
        }
        private void Commit(ILoopDataSource nextSource, RefillOptions options, bool sourceChanged)
        {
            if (reconciling || committing || handlingCellCallbacks) throw new InvalidOperationException("Cell 生命周期回调内不能重入结构更新，请在回调结束后提交。");
            scrollOperationDepth++;
            try
            {
                committing = true;
                try
                {
                    if (nextSource.Count < 0) throw new InvalidOperationException("Count 不能为负。");
                    if (options.AnchorPolicy == ScrollAnchorPolicy.KeepFirstVisible && !nextSource.HasStableKeys && Count > 0)
                        throw new InvalidOperationException("保持可见业务项需要稳定 Key selector。");
                    var count = nextSource.Count;
                    var nextKeys = new string[count];
                    var nextTypes = new int[count];
                    var nextSizes = new float[count];
                    var nextIndices = new Dictionary<string, int>(count, StringComparer.Ordinal);
                    // 先构建并验证完整快照，错误不会破坏当前展示。
                    for (var i = 0; i < count; i++)
                    {
                        var key = nextSource.GetItemKey(i);
                        var type = nextSource.GetCellType(i);
                        var estimate = nextSource.GetEstimatedSize(i, CrossSize);
                        if (string.IsNullOrEmpty(key) || nextIndices.ContainsKey(key)) throw new InvalidOperationException($"Key 为空或重复：index={i}, key={key}");
                        if (!pools.ContainsKey(type)) throw new InvalidOperationException($"未配置 Cell 类型：index={i}, type={type}");
                        if (!Finite(estimate) || estimate <= 0) throw new InvalidOperationException($"估算尺寸必须为有限正数：index={i}");
                        nextIndices.Add(key, i); nextKeys[i] = key; nextTypes[i] = type;
                        nextSizes[i] = (dynamicSize && !sourceChanged && measuredSizes.TryGetValue(key, out var measured) ? measured : dynamicSize ? estimate : DefaultItemSize) + AxisSpacing;
                    }
                    if (options.Index.HasValue && (options.Index.Value < 0 || options.Index.Value >= count)) throw new ArgumentOutOfRangeException(nameof(options));
                    var anchor = CaptureAnchor();
                    EndScroll(ScrollCancelReason.DataChanged);
                    animationDuration = 0; alignedKey = null;
                    followEndDuringMeasurement = options.AnchorPolicy == ScrollAnchorPolicy.StickToEnd;
                    RecycleAll();
                    if (sourceChanged) measuredSizes.Clear();
                    else
                    {
                        var removed = new List<string>();
                        foreach (var pair in measuredSizes) if (!nextIndices.ContainsKey(pair.Key)) removed.Add(pair.Key);
                        for (var i = 0; i < removed.Count; i++) measuredSizes.Remove(removed[i]);
                    }
                    source = nextSource; keys = nextKeys; types = nextTypes; indices = nextIndices;
                    sizes.Reset(nextSizes, count); invalidSizes.Clear();
                    UpdateContentSize();
                    RestoreAnchor(anchor, options.AnchorPolicy);
                    if (options.Index.HasValue)
                    {
                        alignedKey = keys[options.Index.Value]; alignedAlignment = options.Alignment;
                        followEndDuringMeasurement = options.Index.Value == Count - 1 && options.Alignment == ScrollAlignment.End;
                        SetOffset(TargetOffset(options.Index.Value, options.Alignment));
                    }
                    pendingOptions = !isActiveAndEnabled || !HasViewport ? options : (RefillOptions?)null;
                    dirty = true;
                }
                finally { committing = false; }
                if (isActiveAndEnabled) Reconcile();
                DataChanged?.Invoke();
            }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }

        private void RestoreAnchor(Anchor anchor, ScrollAnchorPolicy policy)
        {
            var position = policy == ScrollAnchorPolicy.StickToEnd ? MaxOffset : policy == ScrollAnchorPolicy.ResetToStart ? 0 : anchor.Position;
            if (policy == ScrollAnchorPolicy.KeepFirstVisible && anchor.Index >= 0)
            {
                if (!indices.TryGetValue(anchor.Key, out var index))
                {
                    index = -1;
                    for (var i = anchor.Index + 1; i < anchor.OldKeys.Length; i++)
                        if (indices.TryGetValue(anchor.OldKeys[i], out var candidate)) { index = candidate; break; }
                    if (index < 0) for (var i = anchor.Index - 1; i >= 0; i--)
                        if (indices.TryGetValue(anchor.OldKeys[i], out var candidate)) { index = candidate; break; }
                }
                position = index >= 0 ? ItemStart(index) + anchor.Within : 0;
            }
            SetOffset(position);
        }

        /// <summary>定位数据索引；有效新请求替代旧请求，inactive 或零尺寸时等待恢复。</summary>
        /// <param name="index">0 到 Count-1；无效索引同步报错且保留旧请求。</param>
        /// <param name="alignment">起点、中心或末尾对齐。</param>
        /// <param name="animation">默认立即，有限非正时长立即定位；非有限时长同步报错。</param>
        /// <param name="offsetPixels">最终位置为对齐位置减此值，再钳制；正值使目标向下或右移动。</param>
        /// <param name="onFinished">布局稳定后完成或首次取消时通知一次；通知允许再次定位，异常记录日志。</param>
        public void ScrollToCell(int index, ScrollAlignment alignment = ScrollAlignment.Start, ScrollAnimation animation = default,
            float offsetPixels = 0, Action<ScrollResult> onFinished = null)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (!Finite(offsetPixels)) throw new ArgumentOutOfRangeException(nameof(offsetPixels));
            ValidateAnimation(animation);
            BeginScroll(index, alignment, offsetPixels, 0, animation.Duration, onFinished);
        }
        /// <summary>定位逻辑偏移；有限列表自动钳制，循环布局允许负数，等待和取消语义与索引定位一致。</summary>
        /// <param name="offset">有限 Canvas UI 像素偏移；无效值同步报错且保留旧请求。</param>
        /// <param name="animation">默认立即；非有限时长同步报错。</param>
        /// <param name="onFinished">完成或取消通知一次；IsAnimating 不代表请求已经结束。</param>
        public void ScrollToOffset(float offset, ScrollAnimation animation = default, Action<ScrollResult> onFinished = null)
        {
            if (!Finite(offset)) throw new ArgumentOutOfRangeException(nameof(offset));
            ValidateAnimation(animation);
            Initialize();
            BeginScroll(-1, default, 0, offset, animation.Duration, onFinished);
        }
        private static void ValidateAnimation(ScrollAnimation animation)
        {
            if (!Finite(animation.Duration)) throw new ArgumentOutOfRangeException(nameof(animation));
        }
        private bool HasViewport => ScrollRect.viewport != null && ScrollRect.viewport.rect.width > 0 && ScrollRect.viewport.rect.height > 0;
        private float RequestTarget => ClampOffset(scrollIndex >= 0 ? TargetOffset(scrollIndex, scrollAlignment) - scrollOffsetPixels : scrollOffset);
        private float ClampOffset(float value) => looping ? value : Mathf.Clamp(value, 0, MaxOffset);
        private void BeginScroll(int index, ScrollAlignment alignment, float offsetPixels, float offset, float duration, Action<ScrollResult> callback)
        {
            if (destroying) throw new InvalidOperationException("销毁中的列表不能接收定位请求。");
            scrollOperationDepth++;
            try
            {
                EndScroll(ScrollCancelReason.Replaced);
                hasScrollRequest = true; scrollPending = true;
                scrollIndex = index; scrollAlignment = alignment; scrollOffsetPixels = offsetPixels;
                scrollOffset = offset; scrollDuration = duration; scrollFinished = callback;
                followEndDuringMeasurement = false;
                if (isActiveAndEnabled && HasViewport && !reconciling && !committing && !handlingCellCallbacks)
                {
                    UpdateViewport();
                    StartScroll();
                    if (dirty) Reconcile();
                    TryCompleteScroll();
                }
            }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }
        private void StartScroll()
        {
            scrollPending = false; scrollStartedFrame = Time.frameCount;
            // 启用后业务可能在首个 LateUpdate 前定位，旧重填锚点不能随后覆盖新定位。
            pendingOptions = null;
            alignedKey = null;
            ScrollRect.StopMovement();
            animationFrom = Offset; animationTo = RequestTarget; animationTime = 0;
            animationDuration = Mathf.Max(0, scrollDuration);
            if (!IsAnimating) SetOffset(animationTo);
        }
        /// 取消当前定位，包括等待执行的请求；保留当前位置，无请求时无操作。
        public void CancelAnimation()
        {
            EndScroll(ScrollCancelReason.ExplicitCancel);
            NotifyScroll();
        }
        private void EndScroll(ScrollCancelReason reason)
        {
            if (!hasScrollRequest) return;
            var callback = scrollFinished;
            // 先彻底解除旧状态，再排队通知；通知重入产生的新请求不会被旧请求清理覆盖。
            hasScrollRequest = false; scrollPending = false; scrollFinished = null;
            animationDuration = 0; alignedKey = null; followEndDuringMeasurement = false;
            if (callback != null) scrollNotifications.Add(new ScrollNotification { Callback = callback,
                Result = new ScrollResult(reason == ScrollCancelReason.None ? ScrollStatus.Completed : ScrollStatus.Canceled, reason) });
        }
        private void TryCompleteScroll()
        {
            if (!hasScrollRequest || scrollPending || IsAnimating || reconciling || committing || handlingCellCallbacks) return;
            if (!isActiveAndEnabled) { EndScroll(ScrollCancelReason.Disabled); return; }
            if (!HasViewport) { EndScroll(ScrollCancelReason.ViewportUnavailable); return; }
            if (dirty || ScrollRect.viewport.rect.size != viewportSize || (dynamicSize && Time.frameCount <= scrollStartedFrame)) return;
            if (Mathf.Abs(Offset - RequestTarget) > 1) { SetOffset(RequestTarget); return; }
            EndScroll(ScrollCancelReason.None);
        }
        private void NotifyScroll()
        {
            if (scrollOperationDepth > 0 || reconciling || committing || handlingCellCallbacks || notifyingScroll || scrollNotifications.Count == 0) return;
            notifyingScroll = true;
            try
            {
                for (var i = 0; i < scrollNotifications.Count; i++)
                {
                    var notification = scrollNotifications[i]; scrollNotifications[i] = default;
                    try { notification.Callback(notification.Result); }
                    catch (Exception exception) { Debug.LogException(exception, this); }
                }
            }
            finally { scrollNotifications.Clear(); notifyingScroll = false; }
        }
        private float TargetOffset(int index, ScrollAlignment alignment)
        {
            var start = ItemStart(index);
            var size = IsGrid ? DefaultItemSize : sizes[index] - AxisSpacing;
            return start - (alignment == ScrollAlignment.Center ? (ViewportLength - size) * .5f : alignment == ScrollAlignment.End ? ViewportLength - size : 0);
        }
        private float ItemStart(int index) => looping ? index * loopPageSize : StartPadding + (IsGrid ? (index / Lanes) * Stride : sizes.Prefix(index));
        private int IndexAt(float position) => IsGrid ? Mathf.Clamp(Mathf.FloorToInt((position - StartPadding) / Stride) * Lanes, 0, Count - 1)
            : sizes.Find(Mathf.Max(0, position - StartPadding));
        private void SetOffset(float value)
        {
            value = looping ? value : Mathf.Clamp(value, 0, MaxOffset);
            var position = ScrollRect.content.anchoredPosition;
            if (IsVertical) position.y = value; else position.x = -value;
            ScrollRect.content.anchoredPosition = position;
            if (IsDragging && currentDrag != null)
            {
                // 尺寸/Prepend 修正改变逻辑偏移时，同步原生手势基线及上一帧位置。
                // 只调用公开 API，下一次 OnDrag 不会覆盖锚点修正，也不会产生虚假惯性。
                ScrollRect.OnBeginDrag(currentDrag);
                ScrollRect.Rebuild(CanvasUpdate.PostLayout);
            }
            dirty = true;
        }

        /// <summary>标记绑定后的异步尺寸变化，下一个 LateUpdate 批量测量。</summary>
        /// <param name="index">当前数据索引。</param>
        public void InvalidateCellSize(int index)
        {
            if (!dynamicSize) return;
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            measuredSizes.Remove(keys[index]); invalidSizes.Add(index); dirty = true;
        }
        /// <summary>查询稳定 Key 的当前索引，不分配内存。</summary>
        /// <param name="key">业务 Key。</param>
        /// <param name="index">不存在时为 -1。</param>
        /// <returns>是否存在。</returns>
        public bool TryGetIndex(string key, out int index)
        { if (key != null && indices.TryGetValue(key, out index)) return true; index = -1; return false; }
        /// <summary>获取当前快照的 Key。</summary>
        /// <param name="index">数据索引。</param>
        public string GetItemKey(int index) => keys[index];
        /// <summary>查询已实例化的可见 Cell；业务不得保存为永久数据引用。</summary>
        /// <param name="index">数据索引。</param>
        public LoopCell GetVisibleCell(int index)
        {
            foreach (var pair in active) if (pair.Value.Context.Index == index) return pair.Value;
            return null;
        }
        internal void VisitActiveCells(Action<LoopCell, CellBindContext> visitor)
        { foreach (var pair in active) visitor(pair.Value, pair.Value.Context); }

        private void LateUpdate()
        {
            if (!initialized) return;
            scrollOperationDepth++;
            try
            {
                if (hasScrollRequest && !scrollPending && !HasViewport) EndScroll(ScrollCancelReason.ViewportUnavailable);
                UpdateViewport();
                if (!HasViewport) return;
                if (pendingOptions.HasValue)
                {
                    var options = pendingOptions.Value; pendingOptions = null;
                    if (options.Index.HasValue && options.Index.Value < Count) SetOffset(TargetOffset(options.Index.Value, options.Alignment));
                    else if (options.AnchorPolicy == ScrollAnchorPolicy.StickToEnd) SetOffset(MaxOffset);
                }
                if (hasScrollRequest && scrollPending) StartScroll();
                if (animationDuration > 0)
                {
                    animationTo = RequestTarget;
                    animationTime += Time.unscaledDeltaTime;
                    SetOffset(Mathf.Lerp(animationFrom, animationTo, Mathf.SmoothStep(0, 1, animationTime / animationDuration)));
                    if (animationTime >= animationDuration) animationDuration = 0;
                }
                if (dirty) Reconcile();
                TryCompleteScroll();
            }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }
        private void UpdateViewport()
        {
            if (ScrollRect.viewport == null) return;
            var nextSize = ScrollRect.viewport.rect.size;
            if (nextSize != viewportSize)
            {
                var anchor = CaptureAnchor();
                var oldCross = CrossSize;
                viewportSize = nextSize;
                if (dynamicSize && !Mathf.Approximately(oldCross, CrossSize))
                {
                    measuredSizes.Clear();
                    for (var i = 0; i < Count; i++) sizes.Set(i, Mathf.Max(1, source.GetEstimatedSize(i, CrossSize)) + AxisSpacing);
                    foreach (var pair in active) pair.Value.Measured = false;
                }
                UpdateContentSize();
                RestoreAnchor(anchor, ScrollAnchorPolicy.KeepFirstVisible);
                if (hasScrollRequest && !scrollPending)
                { animationTo = RequestTarget; if (!IsAnimating) SetOffset(animationTo); }
                dirty = true;
            }
        }

        private void UpdateContentSize()
        {
            if (!initialized && ScrollRect.content == null) return;
            ScrollRect.content.sizeDelta = IsVertical ? new Vector2(CrossSize, looping ? loopPageSize * Mathf.Max(1, Count) : ContentLength)
                : new Vector2(looping ? loopPageSize * Mathf.Max(1, Count) : ContentLength, CrossSize);
        }
        private void OnScroll(Vector2 position) { dirty = true; ScrollPositionChanged?.Invoke(); }
        private void HandleClick(LoopCell cell, CellBindContext context)
        {
            scrollOperationDepth++;
            try { if (context.IsCurrent) CellClicked?.Invoke(cell, context); }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }

        private void Reconcile()
        {
            if (reconciling || committing || handlingCellCallbacks || !initialized || !isActiveAndEnabled || !HasViewport || ViewportLength <= 0 || CrossSize <= 0) return;
            reconciling = true;
            try
            {
                dirty = false;
                if (Count == 0) { RecycleAll(); VisibleRange = VisibleRange.Empty; return; }
                int firstSlot, lastSlot;
                if (looping)
                {
                    firstSlot = Mathf.FloorToInt((Offset - overscan) / loopPageSize);
                    lastSlot = Mathf.FloorToInt((Offset + ViewportLength + overscan - .01f) / loopPageSize);
                    VisibleRange = new VisibleRange(Mod(Mathf.FloorToInt(Offset / loopPageSize), Count), Mod(Mathf.FloorToInt((Offset + ViewportLength - .01f) / loopPageSize), Count));
                }
                else
                {
                    firstSlot = IndexAt(Mathf.Max(0, Offset - overscan));
                    lastSlot = IndexAt(Offset + ViewportLength + overscan - .01f);
                    var visibleFirst = IndexAt(Mathf.Max(0, Offset));
                    var visibleLast = IndexAt(Offset + ViewportLength - .01f);
                    if (IsGrid) { lastSlot = Mathf.Min(Count - 1, lastSlot + Lanes - 1); visibleLast = Mathf.Min(Count - 1, visibleLast + Lanes - 1); }
                    VisibleRange = new VisibleRange(visibleFirst, visibleLast);
                }
                recycleSlots.Clear();
                foreach (var pair in active) if (pair.Key < firstSlot || pair.Key > lastSlot || refreshPending) recycleSlots.Add(pair.Key);
                for (var i = 0; i < recycleSlots.Count; i++) Recycle(recycleSlots[i]);
                refreshPending = false;
                if (!isActiveAndEnabled) return;
                for (var slot = firstSlot; slot <= lastSlot; slot++)
                {
                    if (!isActiveAndEnabled) break;
                    var index = looping ? Mod(slot, Count) : slot;
                    if (!active.TryGetValue(slot, out var cell))
                    {
                        var pool = pools[types[index]];
                        cell = pool.Free.Count > 0 ? pool.Free.Pop() : CreateCell(types[index], pool);
                        cell.Slot = slot;
                        cell.transform.SetParent(ScrollRect.content, false);
                        Position(cell, index, slot);
                        var context = cell.BeginBind(keys[index], index, source, cachedClick);
                        active.Add(slot, cell);
                        // 先发布新 context，再激活：业务组件的 Awake 必须在 Bind 前完成。
                        try { cell.gameObject.SetActive(true); source.BindCell(cell, index, context); if (context.IsCurrent) CellBound?.Invoke(cell, context); }
                        catch (Exception exception)
                        {
                            Recycle(slot);
                            Debug.LogError($"Cell 绑定失败：index={index}, key={keys[index]}, type={types[index]}\n{exception}", this);
                            continue;
                        }
                    }
                    else Position(cell, index, slot);
                    if (dynamicSize && (!cell.Measured || invalidSizes.Contains(index))) dirty = true;
                }
                if (dynamicSize) MeasureActive();
                if (!dirty && !IsAnimating && !hasScrollRequest) alignedKey = null;
            }
            finally { reconciling = false; TryCompleteScroll(); NotifyScroll(); }
        }
        private void Position(LoopCell cell, int index, int slot)
        {
            var rect = cell.RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            var axis = looping ? slot * loopPageSize : ItemStart(index);
            var cross = CrossStartPadding + (IsGrid ? (index % Lanes) * (CrossCellSize + CrossSpacing) : 0);
            var width = IsGrid ? cellSize.x : IsVertical ? Mathf.Max(1, CrossSize - CrossStartPadding - CrossEndPadding) : looping ? loopPageSize : sizes[index] - AxisSpacing;
            var height = IsGrid ? cellSize.y : !IsVertical ? Mathf.Max(1, CrossSize - CrossStartPadding - CrossEndPadding) : looping ? loopPageSize : sizes[index] - AxisSpacing;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = IsVertical ? new Vector2(cross, -axis) : new Vector2(axis, -cross);
        }
        private void MeasureActive()
        {
            var anchor = CaptureAnchor();
            var stick = followEndDuringMeasurement || (Offset > 0 && DistanceToEnd <= 1);
            var changed = false;
            foreach (var pair in active)
            {
                var cell = pair.Value;
                var index = cell.Context.Index;
                if (cell.Measured && !invalidSizes.Contains(index)) continue;
                LayoutRebuilder.ForceRebuildLayoutImmediate(cell.RectTransform);
                var value = IsVertical ? LayoutUtility.GetPreferredHeight(cell.RectTransform) : LayoutUtility.GetPreferredWidth(cell.RectTransform);
                if (!Finite(value) || value <= 0) value = IsVertical ? cell.RectTransform.rect.height : cell.RectTransform.rect.width;
                value = Mathf.Max(1, value);
                measuredSizes[keys[index]] = value;
                cell.Measured = true; invalidSizes.Remove(index);
                if (Mathf.Abs(sizes[index] - AxisSpacing - value) > .01f) { sizes.Set(index, value + AxisSpacing); changed = true; }
            }
            if (changed)
            {
                UpdateContentSize();
                if (hasScrollRequest && !scrollPending)
                {
                    animationTo = RequestTarget;
                    if (!IsAnimating) SetOffset(animationTo);
                }
                else if (alignedKey != null && indices.TryGetValue(alignedKey, out var targetIndex))
                {
                    var target = TargetOffset(targetIndex, alignedAlignment);
                    if (IsAnimating) animationTo = Mathf.Clamp(target, 0, MaxOffset); else SetOffset(target);
                }
                else RestoreAnchor(anchor, stick ? ScrollAnchorPolicy.StickToEnd : ScrollAnchorPolicy.KeepFirstVisible);
                foreach (var pair in active) Position(pair.Value, pair.Value.Context.Index, pair.Key);
                dirty = true;
            }
        }
        private LoopCell CreateCell(int type, Pool pool)
        {
            var cell = Instantiate(pool.Prefab, poolRoot);
            cell.gameObject.SetActive(false); cell.PoolType = type; CreatedCellCount++;
            return cell;
        }
        private void Recycle(int slot)
        {
            if (!active.TryGetValue(slot, out var cell)) return;
            // 先移出活跃表；业务解绑触发父节点隐藏时，不会重复回收同一个实例。
            active.Remove(slot);
            var context = cell.Context;
            var previousHandling = handlingCellCallbacks;
            handlingCellCallbacks = true;
            try
            {
                cell.EndBind();
                try { CellUnbound?.Invoke(cell, context); }
                catch (Exception exception) { Debug.LogException(exception, this); }
                if (cell != null && poolRoot != null)
                {
                    cell.gameObject.SetActive(false); cell.transform.SetParent(poolRoot, false);
                    pools[cell.PoolType].Free.Push(cell);
                }
            }
            finally { handlingCellCallbacks = previousHandling; }
        }
        private void RecycleAll()
        {
            recycleSlots.Clear(); foreach (var pair in active) recycleSlots.Add(pair.Key);
            for (var i = 0; i < recycleSlots.Count; i++) Recycle(recycleSlots[i]);
        }
        /// <summary>进入 Carousel 内部循环布局；固定 List 专用。</summary>
        /// <param name="pageSize">固定主轴页尺寸。</param>
        internal void EnableLoop(float pageSize)
        {
            Initialize();
            if (IsGrid || dynamicSize || pageSize <= 0) throw new InvalidOperationException("Carousel 需要固定尺寸 List。");
            if (!looping)
            {
                savedMovement = ScrollRect.movementType; savedHorizontalBar = ScrollRect.horizontalScrollbar; savedVerticalBar = ScrollRect.verticalScrollbar;
                ScrollRect.movementType = ScrollRect.MovementType.Unrestricted;
                if (IsVertical) ScrollRect.verticalScrollbar = null; else ScrollRect.horizontalScrollbar = null;
            }
            looping = true; loopPageSize = pageSize; UpdateContentSize(); dirty = true;
        }
        internal void DisableLoop()
        {
            if (!looping) return;
            looping = false; ScrollRect.movementType = savedMovement;
            ScrollRect.horizontalScrollbar = savedHorizontalBar; ScrollRect.verticalScrollbar = savedVerticalBar;
            RecycleAll(); UpdateContentSize(); SetOffset(0); dirty = true;
        }
        internal void RecenterLoop(int virtualPage, float pageSize)
        {
            if (!looping || Count < 2 || IsDragging || IsAnimating) return;
            var page = Mod(virtualPage, Count);
            if (page == virtualPage) return;
            RecycleAll(); ScrollRect.StopMovement(); SetOffset(page * pageSize - (ViewportLength - pageSize) * .5f); dirty = true;
        }
        /// <summary>拖动开始时取消定位动画。</summary>
        /// <param name="eventData">Unity 指针事件。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            scrollOperationDepth++;
            try { IsDragging = true; currentDrag = eventData; EndScroll(ScrollCancelReason.DragStarted); alignedKey = null; followEndDuringMeasurement = false; DragStarted?.Invoke(); }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }
        /// <summary>结束拖动并通知扩展组件。</summary>
        /// <param name="eventData">Unity 指针事件。</param>
        public void OnEndDrag(PointerEventData eventData) { IsDragging = false; currentDrag = null; followEndDuringMeasurement = DistanceToEnd <= 1; DragEnded?.Invoke(); }
        private void OnEnable() { dirty = true; }
        private void OnDisable()
        {
            scrollOperationDepth++;
            try
            {
                IsDragging = false; currentDrag = null;
                if (!scrollPending) EndScroll(ScrollCancelReason.Disabled);
                alignedKey = null;
                if (initialized) RecycleAll(); VisibleRange = VisibleRange.Empty;
            }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }
        private void OnDestroy()
        {
            destroying = true;
            scrollOperationDepth++;
            try
            {
                EndScroll(ScrollCancelReason.Destroyed);
                if (!initialized) return;
                ScrollRect.onValueChanged.RemoveListener(OnScroll); RecycleAll();
                if (poolRoot != null) { if (Application.isPlaying) Destroy(poolRoot.gameObject); else DestroyImmediate(poolRoot.gameObject); }
                pools.Clear(); measuredSizes.Clear(); source = null;
            }
            finally { scrollOperationDepth--; NotifyScroll(); }
        }
        internal static int Mod(int value, int count) => ((value % count) + count) % count;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
