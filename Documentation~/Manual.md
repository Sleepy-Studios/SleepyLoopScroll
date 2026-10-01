# Manual

## 层级

```text
Canvas
  ScrollRect + LoopScrollView
    Viewport + RectMask2D + 可射线命中的 Graphic
      Content（仅 RectTransform，插件定位）
    CellTemplate（隐藏，LoopCell + 业务组件）
    LoopCellPool（运行时创建并隐藏）
```

显式指定 ScrollRect 的 Viewport 和 Content。列表仅使用一个滚动轴。Content 和 Cell 使用左上角锚点/pivot；业务不要修改 Content 坐标或插件分配的 Cell 位置。

## Inspector

`Layout` 选择 Vertical/Horizontal/VerticalGrid/HorizontalGrid。`Cell Size` 是固定尺寸或动态 List 的初始估算。`Spacing` 非负；`Padding` 为四边内距；`Overscan` 为 Viewport 外的预加载距离。Grid 使用 Viewport 横轴可用长度计算每行/列数量，至少一个。

Cell Prefabs 中的 Type 必须唯一，Prefab 根节点包含 LoopCell 和 RectTransform。简单 SetData 使用 Type 0。多类型数据源返回相应整数 Type，不能返回未配置值。Prewarm 按类型分别配置；覆盖最大 Viewport 和 Overscan 所需对象数量后，滚动不再创建实例。

Content 上的 LayoutGroup/ContentSizeFitter 是错误配置；Inspector 和首次提交数据都会报告。Cell 内部可使用布局组件，不需要为此移除子级布局。

## 动态 List

开启 Dynamic Size，高级数据源根据 `crossAxisSize` 返回主轴估算尺寸。首次可见绑定后使用 LayoutUtility 的 preferred size 测量；没有 preferred size 时使用 Cell 当前 rect。推荐在 Cell 根节点使用 LayoutElement，或让布局组提供内容 preferred size。

异步文本、图片使尺寸变化后调用 `InvalidateCellSize(context.Index)`，先确认 `context.IsCurrent`。测量请求合并到下一次 LateUpdate。Viewport 横轴变化会清空测量缓存并重新估算，随后只测量可见 Cell。Grid 首版不支持动态尺寸。

## 隐藏与复用

inactive 时可提交数据；这不会创建可见 Cell。OnEnable 后完整恢复，可从 0 Cell 重建。禁用会取消活跃绑定；池和数据保留。回收后旧上下文失效，不能继续使用旧索引或 Cell 引用写入数据。

每次租出先建立 context，再激活 Cell，确保业务组件 Awake 已完成，然后执行 Bind。OnEnable 适合通用表现初始化；与当前业务数据相关的异步加载从 Bind 启动。所有列表/UI API 都在 Unity 主线程调用。
