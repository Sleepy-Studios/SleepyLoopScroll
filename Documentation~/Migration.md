# 钓鱼调用习惯迁移

正式用法是先注册一次，再提交真实集合。以下为调用语义对照，迁移时直接修改调用方。

| 钓鱼用法 | 正式接口 |
|---|---|
| RegisterLoopScrollRect<MyItemView>(list, callback) | 宿主同名泛型入口配置工厂，回调统一 `(ItemView,index,CellBindContext)`。 |
| RegisterLoopScrollClick<MyItemView> | RegisterLoopScrollClick，回调同上；工厂由绑定注册配置。 |
| RegisterLoopScrollItemHide<MyItemView> | RegisterLoopScrollItemHide，回调 `(ItemView,CellBindContext)`。 |
| SetTotalCount(items) | 同名提交真实 IList；null 清空，默认起点。 |
| RefreshCells() | 同名重绑活跃项；数量变化用 RefillCells 或 ApplyChanges。 |
| RefillCells(index) | RefillCells(new RefillOptions(index))。 |
| RefillCellsFromEnd() | RefillCells(new RefillOptions(ScrollAnchorPolicy.StickToEnd))。 |
| ScrollToCell(index) | 同名定位；对齐用 ScrollAlignment，动画用时长。 |
| autoSelect | 独立 LoopSelectionController，使用稳定 Key。 |
| 修改 Content 坐标 | ScrollToCell 或锚点策略。 |
| 平行类型/尺寸集合 | 高级 DataSource 统一回答 Key、类型与尺寸。 |

包级简单列表使用 RegisterCellBinding<TCell> 配置 Unity Component 绑定；宿主 ItemView 是普通 C# 类，通过 Core.Runtime 的三种注册入口接入。MvcBind 只生成 On{FieldName}RectData/Click/ItemHide；非泛型生成代码需在提交前 ItemViews().Configure<TView>()。所有回调都有 context，同步业务可忽略，异步写入前检查 IsCurrent。

稳定 Key 来自业务 ID，不依赖语言和索引。不恢复 forceRefill、旧速度、多 bool 参数；inactive 和 0 Cell 恢复由生命周期保证。本次不修改 fishinggameplay。
