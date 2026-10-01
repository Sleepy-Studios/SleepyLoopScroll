# Changelog

## Unreleased — 示例与 API 易用性

- 一个 Showcase 导入入口，五个可导航场景及返回按钮；Editor 不依赖 Build Settings。
- 首次中文、持久化中英文切换、稳定 ID 数据与独立 Noto Sans SC 字体。
- 统一 RegisterCellBinding 后 SetTotalCount；操作入口为 RefreshCells、RefillCells(RefillOptions)、ScrollToCell。
- 删除平行方法与重复简单数据源；保留稳定锚点、异步隔离和高级 DataSource。宿主仅三种带 context 的注册与 MvcBind 回调。

## 0.1.0 — 2026-10-01 本地实现，尚未发布

- 原生 ScrollRect 组合的虚拟 List、固定 Grid、类型独立对象池。
- 动态尺寸 Fenwick 索引、稳定 Key 锚点与集合变更通知。
- 懒创建取消 Token 和绑定版本隔离。
- Chat、Carousel、Paging、Selection、Nested Scroll 扩展。
- 四个独立 Samples、Editor 创建入口、Inspector 配置检查及包测试。
- Unity 2022.3/6000.3 实测；使用经校准原生 GC.Alloc recorder 的 60 秒零分配基准。
- inactive 宿主桥接初始化、Awake 先于绑定、动态定位/目标重载对齐、拖动中前插和生命周期重入防护。
