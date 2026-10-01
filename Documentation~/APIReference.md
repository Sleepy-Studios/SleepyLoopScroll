# API Reference

## 简单数据

`SetData<TItem,TCell>(IReadOnlyList<TItem>, Action<TCell,TItem,CellBindContext>, Action<TCell,CellBindContext>, ReloadOptions)`：类型 0 的组件绑定。TCell 是 Unity Component。省略解绑回调与 options 时默认完整重载并回到起点。

稳定 Key 重载在 unbind 后增加 `Func<TItem,string> getItemKey`。不提供 selector 时使用位置字符串 Key，仅代表当前快照；不能用于跨重排的身份保持或 Selection。

## 高级数据源

实现 `ILoopDataSource`：Count、HasStableKeys、GetItemKey、GetCellType、GetEstimatedSize、BindCell、UnbindCell。Key 非空唯一；尺寸为有限正数；类型已配置。数据源不负责实例化或回收。

Bind 是同步入口，可以启动宿主自己的异步任务。`CellBindContext` 包含 Index、Key、Version、IsCurrent、CancellationToken。Token 首次读取才创建；失效上下文返回取消 Token。Unbind 时旧 context 已失效。

## 更新与所有权

调用方持有并修改集合，然后通知列表；通知之前不要对已修改数据调用 RefreshVisible。

| API | 语义 |
|---|---|
| SetDataSource | 更换数据源、验证完整快照、清空测量缓存并重载。 |
| ReloadData | 重新读取 Count/Key/Type/估算尺寸并完整重绑，默认起点。 |
| RefreshVisible | 重新绑定当前活跃项；不修改数量。 |
| ApplyChanges | 按序验证变更描述与最终 Count，一次 reconcile。 |
| Append/Prepend | 调用方追加/前插后的 Insert 通知，count 必须为正。 |
| InvalidateCellSize | 动态尺寸失效，下一次布局批量测量。 |

LoopListChange 有 Insert/Remove/Replace/Move。Index 以该描述之前的逻辑状态计算。Move 的 Destination 是移除 Count 项后再插入的索引。批次结构不合法、最终数量不一致、重复 Key 或未知类型会在改变展示前抛异常。列表不回滚调用方已修改的集合。

绑定/解绑及 Cell 生命周期回调内不得重入结构更新或 RefreshVisible，需回调结束后提交。绑定失败清理该 Cell 并记录索引、Key、类型；下一次刷新可重试。

## 锚点与定位

ScrollAnchorPolicy：ResetToStart、KeepPosition、KeepFirstVisible、StickToEnd。KeepFirstVisible 需要稳定业务 Key；删除锚点时选旧顺序中最近存活后继，无后继选前驱。保持主轴像素偏移后钳制到合法范围。

ReloadOptions 默认 ResetToStart。可用 `new ReloadOptions(policy)` 或 `new ReloadOptions(index, alignment)`。ScrollTo(index, Start/Center/End, animation) 默认立即；边界处采用可达到的合法位置。ScrollAnimation(duration) 使用 unscaled time 和 SmoothStep。新请求、拖拽、禁用、结构更新取消旧动画。

VisibleRange 为包含式 First/Last；空时均为 -1，包含实际 Viewport 范围而非整个 Overscan 池。Carousel 返回真实索引，跨循环边界时 First 可大于 Last。

CellBound/CellUnbound/CellClicked 支持宿主桥接；点击只使用当前有效绑定。DataChanged 是成功快照提交事件，ScrollPositionChanged 用于边界与未读逻辑。
