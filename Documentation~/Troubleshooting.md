# Troubleshooting

| 现象 | 检查 |
|---|---|
| 配置报错/Cell 位置冲突 | Content 移除 LayoutGroup/ContentSizeFitter；保留 Cell 内部布局。 |
| 0 Cell | Viewport 是否为零尺寸、对象是否 inactive、Prefab 映射是否完整。提交数据后重新启用会完整 reconcile。 |
| 隐藏期间更新无可见对象 | 正常；数据已提交，启用后创建可见项。不要自己取消数据提交。 |
| 异步头像写到其它项 | 完成后检查 context.IsCurrent；Token 只能请求取消，不能约束不配合的异步库。 |
| 动态高度不更新 | Cell 要提供 preferred size；异步内容改变后 InvalidateCellSize。 |
| 高度抖动 | 估算值应接近实际；避免 preferred size 与当前分配高度互相循环依赖。 |
| 锚点身份错乱 | 使用唯一稳定业务 ID；位置 Key 不用于跨重排保持身份。 |
| 滚动仍创建实例 | Prewarm 覆盖每个类型的最大可见数量和 Overscan；窗口变大可增加需求。 |
| Carousel 无 Scrollbar | 无限循环没有有限归一化进度，循环轴主动断开 Scrollbar。 |
| 分页错误后不继续请求 | Error 需要宿主显式 Retry；Completed 需新查询 ResetBoundary。 |
| 嵌套手势无效 | Router 放在子 Viewport，确认射线命中和父子引用。 |

绑定失败日志包含 index/key/type。修正业务配置后重新刷新。调用方集合变化不能仅调用 RefreshVisible，否则快照与集合已不一致。
