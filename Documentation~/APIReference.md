# API Reference

## 注册与提交

`RegisterCellBinding<TCell>(bind, unbind = null)` 配置一次绑定。TCell 是类型 0 Prefab 上的 Unity Component。bind 签名为 `(TCell cell, int index, CellBindContext context)`，unbind 为 `(TCell cell, CellBindContext context)`。业务按索引读取自有集合。重新注册供下一次提交使用，活跃 Cell 仍按原配置解绑。

`SetTotalCount(IList items, RefillOptions options = default, Func<object,string> getItemKey = null)` 提交 List<T> 或数组，null 清空；未配置绑定时报错。每次完整读取并重填，默认回起点，相同集合引用也不会跳过。selector 返回唯一稳定业务 ID；省略时使用快照位置身份，不能用于跨重排锚点或 Selection。

## 高级数据源

`SetDataSource(ILoopDataSource source, RefillOptions options = default)` 提交高级数据源。实现 Count、HasStableKeys、GetItemKey、GetCellType、GetEstimatedSize、BindCell、UnbindCell。Key 非空唯一，尺寸有限且为正，类型已配置。源不负责实例化或回收。

Bind 是同步入口，可启动业务异步任务。`CellBindContext` 包含 Index、Key、Version、IsCurrent、CancellationToken；Token 按需创建。回收、重绑、禁用或销毁立即使旧上下文失效并取消 Token。异步写入前始终检查 IsCurrent。Unbind 收到的 context 已失效。

## 更新与所有权

调用方先修改集合，再通知列表；通知之前不能刷新已修改的集合。

| API | 语义 |
|---|---|
| RefreshCells() | 重绑活跃项，不改变快照数量；inactive 时延后到启用。 |
| RefillCells(RefillOptions options = default) | 重新读取完整数据源、尺寸并重绑，默认起点。 |
| ApplyChanges(changes, anchorPolicy) | 有序验证变更及最终数量，统一 reconcile。 |
| Append/Prepend(count, anchorPolicy) | 调用方追加/前插后的 Insert 通知。 |
| InvalidateCellSize(index) | 下一次布局批量测量动态尺寸。 |

LoopListChange 包含 Insert/Remove/Replace/Move，索引按批次顺序解释。Move 的 Destination 是移除 Count 项后的插入位置。无效批次、重复 Key、未知类型在改变展示前报错；列表不回滚调用方集合。Cell 生命周期回调内不得重入结构更新或刷新。绑定失败清理当前 Cell 并报告 index/key/type。

## 锚点与定位

`RefillOptions` 默认 ResetToStart；`new RefillOptions(policy)` 选择锚点，`new RefillOptions(index, alignment)` 指定目标。贴底使用 `new RefillOptions(ScrollAnchorPolicy.StickToEnd)`。

ScrollAnchorPolicy：ResetToStart、KeepPosition、KeepFirstVisible、StickToEnd。KeepFirstVisible 要求稳定 Key；锚点删除后选择旧顺序中存活后继，无后继选前驱，最后钳制。

正式定位 API 直接扩展现有方法，没有平行入口或兼容重载：

```csharp
void ScrollToCell(int index, ScrollAlignment alignment = ScrollAlignment.Start,
    ScrollAnimation animation = default, float offsetPixels = 0,
    Action<ScrollResult> onFinished = null);
void ScrollToOffset(float offset, ScrollAnimation animation = default,
    Action<ScrollResult> onFinished = null);
```

Start/Center/End 的最终位置均为 `对齐位置 - offsetPixels`，再钳制到有限列表的合法范围。正值让目标相对对齐线向下/右移动，负值反向；单位为 Canvas UI 像素，与屏幕缩放后的物理像素不同。默认 0 保持原行为；RefillOptions 没有额外偏移字段。循环 Carousel 的偏移允许负数。

ScrollAnimation(duration) 使用 unscaled time 和 SmoothStep；有限非正时长立即定位。无效索引、非有限 offset/offsetPixels/Duration 同步抛 ArgumentOutOfRangeException，不创建请求也不取消旧请求，包括直接修改 Duration 字段的情况。

每个列表同时只有一个定位请求，新有效请求替代旧请求。`ScrollResult` 是只读值类型，Status 为 ScrollStatus.Completed/Canceled；CancelReason 为 ScrollCancelReason。每个带回调请求恰好一次终止通知：

| CancelReason | 触发条件 |
|---|---|
| None | 正常完成 |
| Replaced | 被有效的新定位替代 |
| DragStarted | 原生拖动开始 |
| ExplicitCancel | CancelAnimation()，包括等待执行的请求 |
| DataChanged | 数据提交、重填或结构变更通过完整验证 |
| Disabled | 执行中的列表被禁用；Unity 销毁可能先触发此原因 |
| Destroyed | 尚未终止的请求在销毁时取消 |
| ViewportUnavailable | 执行中的 Viewport 任一轴变为零 |

取消停止定位并保留当前位置，拖动交回原生 ScrollRect。CancelAnimation 无请求时无操作；RefreshCells 和 InvalidateCellSize/测量修正不取消。无效数据提交不取消旧请求。

inactive、组件禁用或 Viewport 任一轴为零时提交的请求等待恢复；保留对齐、偏移和完整动画时长，不提前完成。等待请求与数据重填锚点分开维护；数据更新仍取消等待请求。正常非零 Viewport 尺寸变化和动态测量重新计算目标，保留 offsetPixels。

固定尺寸立即定位可以同步完成。动画和动态尺寸定位在最终 reconcile 结束、当前布局稳定且位置误差不超过 1 UI 像素后完成；动态立即请求至少等当前帧之后确认收敛。完成不等待未来异步内容，后续尺寸变化不重复通知。IsAnimating 只表示插值正在运行，等待执行/布局稳定期间可为 false，业务应使用 onFinished 判断结束。

通知前清除旧请求状态，等 Commit/Reconcile/Cell 回调保护退出后执行。通知内可以提交新定位、刷新或重填；旧请求清理不会覆盖新请求。通知异常用 Debug.LogException 记录，不破坏后续请求；通知记录容器复用。无回调路径不创建 Task、CTS、请求对象或逐次闭包。

VisibleRange 为包含式 First/Last，空时 -1；只包含实际 Viewport，而非整个 Overscan。Carousel 返回真实索引，跨循环边界时 First 可大于 Last。

CellBound/CellUnbound/CellClicked 支持桥接；DataChanged 表示成功提交，ScrollPositionChanged 服务边界与未读逻辑。
