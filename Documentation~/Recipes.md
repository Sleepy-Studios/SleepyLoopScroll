# Recipes

## 背包与选择

单类型先 RegisterCellBinding 再 SetTotalCount；多类型实现 GetCellType。用业务 ID 的字符串作 Key。添加 LoopSelectionController，通过 CellSelectionChanged 更新描边/颜色，SelectionChanged 读取 SelectedKeys。选择刷新不重新绑定 Cell，避免中断资源加载。

## Chat

在列表根节点加 LoopChatController。首次非空数据定位末尾。修改集合后用 PrependHistory(count) 或 AppendMessages(count)。距离底部不超过 1 UI 像素时追加贴底；查看历史时保持锚点并累计未读。JumpToLatest 清零未读。

启用动态尺寸，多种消息用高级数据源与对应 Prefab。异步更新先检查 context.IsCurrent，再更新布局并调用 InvalidateCellSize。

## Carousel

固定尺寸 List 加 LoopCarouselController，Configure(pageSize, autoPlayInterval, snapDuration)。页码 0-based；Next/Previous/SetPage 控制翻页。0 项无页码，1 项静止，2 项及以上通过虚拟页索引循环。多项滚动为 Unrestricted，循环轴不连接 Scrollbar。自动播放暂停于拖拽、惯性及吸附期间，坐标回正仅在手势和动画结束后发生。

## Paging

加 LoopPagingTrigger，通过 Configure(start,end,thresholdViewports) 配置边界。LoadRequested 触发前状态已是 Loading。宿主完成网络请求并修改集合，用 Append/Prepend 通知列表，最后 Complete(boundary,hasMore)。失败调用 Fail，显式 Retry；更换查询前先由宿主取消旧请求，再 ResetBoundary。

列表为空也可请求第一页；起点/终点各有独立状态。网络协议、页码、请求取消及过期回包检查由业务负责，组件不隐藏实现网络逻辑。

## Nested Scroll

NestedScrollRouter 挂子 Viewport，显式配置 child/parent ScrollRect。子区域必须可射线命中。正交滚动按主拖动方向选路；同轴到边界后向父交接。循环 Unrestricted 子列表在自己的轴上不做边界交接。路由器转发开始、拖动、结束，每个手势最多交接一次。
