# Architecture

Runtime 由公开契约、LoopScrollView、LoopCell、尺寸索引和独立扩展组成。Editor 仅配置检查、创建层级与导入示例场景生成；Runtime 不引用 Editor。

RegisterCellBinding/SetTotalCount 的索引模式通过独立数据源适配器进入同一个 Commit/Reconcile 流程。每次提交保存独立的绑定与解绑委托，后来注册新配置不会污染仍活跃的旧绑定。公开 API 只有一套名称，简单集合统一由 ListDataSource 进入虚拟化流程。

Showcase 只位于 Samples~，共享程序集负责示例导航、文本和字库；包 Runtime/Editor 不引用它。Catalog 保存子场景相对路径，Editor 从 Catalog 实际资源位置解析导入目录，Player 从显式构建场景中解析。宿主通过场景目录追加 MvcBind 入口，包不引用宿主。

ScrollRect 拥有原生拖动、速度、弹性和 Scrollbar，LoopScrollView 拥有数据快照、逻辑尺寸、活跃范围、类型池及绝对定位。没有复制或继承改写 ScrollRect。

完整提交先验证下一份 Key/Type/尺寸快照，再取消动画和旧绑定、更新尺寸索引与锚点，最后 reconcile 可见范围。错误批次不改变展示。Insert/Remove 等重建索引为 O(n)；不引入复杂平衡树。

动态 List 的前缀尺寸由 Fenwick Tree 维护：查找可见项、单项尺寸更新 O(log n)。测量按首次绑定/显式失效进入队列，只测量当前活跃项。横轴尺寸变化全体重新估算，逐项测量。

Cell 池按整数类型独立。物理 Cell 持有递增版本、当前 Key 与懒创建 CTS。旧 context 保存版本，因此重绑后 IsCurrent 永远为 false。回收先失效/取消，再调用数据源 Unbind 与宿主事件。

Carousel 使用虚拟槽位映射真实数据索引，允许同一业务项同时出现在多个物理 Cell。仅在拖动和吸附结束后回正，避免修改 ScrollRect 手势基线。Chat、Paging、Selection、NestedScroll 不进入核心可见项查找算法。

拖动期间 Prepend 或尺寸变化需要修正偏移时，通过原生公开 OnBeginDrag 与 PostLayout Rebuild 同步手势基线和上一帧位置，避免下一次拖动覆盖修正或把布局变化计入惯性；不读取或改写 ScrollRect 私有字段。

数据与网络归宿主；集合本体不复制，布局快照单独维护。包不引用业务资源框架。测试程序集单向依赖生产程序集，普通 Player 不包含测试程序集。
