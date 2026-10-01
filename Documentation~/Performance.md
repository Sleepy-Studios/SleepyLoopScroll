# Performance

固定列表的可见索引查找 O(log n)，可见 Cell 定位与绑定与 Viewport 数量同阶。十万条数据会有十万条 Key/Type/尺寸快照，不会有十万个 GameObject。完整重填 和结构变更允许 O(n) 重建。

## 零 GC 基准

同步业务绑定、不读取 CancellationToken，预热所有类型以及最大 Viewport/Overscan 后连续滚动 60 秒。测试记录同步 ScrollToCell/Reconcile 当前线程分配以及 CreatedCellCount；要求内部持续分配为 0、实例数不增加。Unity UI/native 事件、TMP/业务异步内容应使用 Profiler 单独归因，不把外部开销算成包内部零 GC 承诺。

测量使用经过已知分配校准的原生 `ProfilerRecorder` / `GC.Alloc`，仅收集调用线程，并只在同步定位调用区间启用。Unity 2022.3 的 `GC.GetAllocatedBytesForCurrentThread()` 可能恒返回 0，不作为通过依据。参考 [Unity recorder 示例](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread.html) 和 [UUM-100690](https://issuetracker.unity.com/issues/5660/crash-on-runtimefieldinforesolvetype-with-il2cpp-and-returns-0-with-mono-when-calling-gcgetallocatedbytesforcurrentthread-method)。

异步读取 Token 时每次绑定最多创建一个 CTS，取消并释放后不复用已取消的 CTS；必要分配单独统计。即使 Token 取消，宿主也必须检查 context.IsCurrent。

Viewport 扩大或新增未预热类型可创建更多对象；这不属于固定测试配置下的稳态滚动。首次导入、第一次绑定、集合结构变更、尺寸缓存重建也不属于稳态基准。

动态布局成本由 Cell 内容布局决定；插件不逐帧扫描全部数据测量。需要稳定估算尺寸及显式失效，以减少锚点修正。
