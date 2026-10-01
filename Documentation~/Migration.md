# Migration

这里只提供调用语义迁移，不提供旧 LoopScrollRect 二进制兼容层。

| 旧 API/习惯 | 新 API |
|---|---|
| SetTotalCount | SetData 或 SetDataSource，直接提交真实集合/数据源。 |
| RefreshCells | RefreshVisible；数量变化用 ApplyChanges/ReloadData。 |
| RefillCells | ReloadData(options)，显式指定位置策略。 |
| ScrollToCell | ScrollTo(index, alignment, animation)。 |
| forceRefill | 不再需要；空 Cell 和 inactive 恢复是内部生命周期契约。 |
| 修改 content.anchoredPosition | ScrollTo 或 ScrollAnchorPolicy。 |
| 平行类型/尺寸/数据集合 | 高级数据源统一回答类型、Key、尺寸。 |

宿主 ItemView 是普通 C# 类，不能直接作为包 SetData 的 TCell。SleepyDemos 用 Core.Runtime 的 ItemViews().SetItems<TItem,TView>() 薄桥接适配。其它项目自行编写对应桥接，包保持独立。

不修改 fishinggameplay。旧调用审计仅用于易用性和行为要求；本包独立设计实现。
