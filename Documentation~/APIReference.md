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

`ScrollToCell(index, alignment = Start, animation = default)` 定位，支持 Start/Center/End；边界按可达范围钳制。ScrollAnimation(duration) 使用 unscaled time 和 SmoothStep。新定位、拖拽、禁用和结构更新取消旧动画。`ScrollToOffset` 保留给循环轮播等偏移定位功能。

VisibleRange 为包含式 First/Last，空时 -1；只包含实际 Viewport，而非整个 Overscan。Carousel 返回真实索引，跨循环边界时 First 可大于 Last。

CellBound/CellUnbound/CellClicked 支持桥接；DataChanged 表示成功提交，ScrollPositionChanged 服务边界与未读逻辑。
