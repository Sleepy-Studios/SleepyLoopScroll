# API 收口验证

日期：2026-10-01。代码已在本地统一，尚未发布；2026-10-01 22:17 后真实 Unity 6 宿主恢复响应，编译和直接相关自动化验收已补齐。真实触摸/Player 构建、临时目录清理与发布授权仍按检查表执行。初版历史见 Validation，示例调整阶段历史见 ShowcaseValidation；当前正式 API 以 APIReference 为准。

## 实现范围

- 包 Runtime 只保留 RegisterCellBinding、SetTotalCount、SetDataSource、RefreshCells、RefillCells(RefillOptions)、ScrollToCell 及集合变更、动态尺寸与偏移定位能力。没有旧名转发、过渡重载或 Obsolete。
- 简单集合只使用 IList 的 ListDataSource，删除 RegisteredListDataSource。提交保存绑定配置，后续注册不改变活跃项的原解绑回调。
- Core.Runtime 只保留 RegisterLoopScrollRect/Click/ItemHide；绑定/点击为 (ItemView,index,CellBindContext)，解绑为 (ItemView,CellBindContext)。仅泛型绑定入口指定工厂；View 和嵌套 ItemView 复用同一订阅逻辑。
- 删除 SetItems、平行 ItemBound/ItemHidden/ItemClicked、LegacyAdapter 和委托转换缓存。MvcBind 仅三种正式约定。
- 迁移包扩展、四个子场景、宿主示例和相关测试。已有示例脚本/场景 GUID 对照通过，宿主导入脚本与包一致。两套性能基准合并为注册路径的一套。

## 编译与自动化

| 范围 | 2022.3.62f3 / uGUI 1.0.0 | 6000.3.15f1 / uGUI 2.0.0 |
|---|---|---|
| 包和纯包示例导入/编译 | 临时宿主通过 | 独立临时宿主通过 |
| 包 PlayMode | 26/26，d775a8e9 | 26/26，6796f087 |
| 包 EditMode 算法 | 2/2，08ede1ec | 2/2，c38804c2 |
| 纯包 Showcase | 5/5，080673c9，加尺寸补验 1/1，9598b1f1 | 6/6，fecab84b |
| 真实 Core/Hotfix、MvcBind 与宿主第六入口 | SleepyDemos 为 Unity 6；此宿主未在 2022 运行 | 正式编译通过；生成 2/2、桥接 5/5、宿主 Showcase 6/6、边界 5/5 |

临时验证使用本地已缓存依赖，Test Framework 分别为 1.1.33 和 1.6.0；真实 Unity 6 宿主的 Core.Runtime、Core.Editor、Hotfix、Tests.EditMode、Tests.PlayMode 最新程序集均于 22:17 完成正式编译，测试结束 Console Error 为 0。

Showcase 覆盖五场景进入/返回、直接运行子场景、连续十轮往返、快速重复请求、缺失场景恢复、中英持久化、选择/聊天/页码身份、中文字体、按钮与原生拖拽事件，以及 1280×720、720×1280、1920×1080 尺寸。2022 首次类筛选只返回五项，重新发现测试后精确补跑尺寸方法。按钮和拖拽为 Test Runner 事件模拟，不声称完成真实触摸手感验收。

2022 回归后只整理两项测试方法名称及 XML 注释；最终 Editor 已再次完成编译；动态重填对齐 1/1（b0e9bd6d）和连续变更/重填锚点 1/1（41614fe2）均补验通过。包运行行为未再修改。测试没有运行无关项目全量测试，也没有 dotnet/msbuild 或 BatchMode 测试包装器。

## 性能

一次注册路径、固定同步绑定、不读取 Token，预热 50 个实例后测试 100000 项连续 60 秒。原生 GC.Alloc recorder 只收集同步 ScrollToCell/Reconcile 调用区间：

| 版本 | 定位次数 | 分配 | 实例数 |
|---|---:|---:|---:|
| 2022.3 | 8622 | 0 B | 50，未增加 |
| 6000.3 | 8641 | 0 B | 50，未增加 |

异步 Token、业务/TMP、帧间 Unity/Editor 开销不混入该口径。锚点、动态定位与异步过期隔离包含在上述包回归中，位置断言容差为 1 UI 像素。

## 证据和待办

原始 JSON、Editor 日志及截图位于忽略目录 ValidationArtifacts~，文件名前缀 unified-2022 / unified-6000。截图位于 showcase 子目录；本轮已检查 2022 中文主菜单/竖屏以及 Unity 6 中文聊天/轮播截图，文字显示正常。

此前 SleepyDemos 主线程停滞，本轮重试已恢复；未重启真实宿主。通过 Assets/Refresh 确认最新代码正式编译后，串行运行以下 Unity Test Runner 类，全部 18/18 通过：

- LoopScrollMvcGenerationTests：2/2，7b16551f，仅三种带 context 的正式回调发现/生成。
- LoopScrollItemViewBridgeTests：5/5，067a8289，工厂、注册去重、ItemView 复用、解绑/点击身份、过期隔离、嵌套订阅释放和 inactive 恢复。
- LoopScrollShowcaseTests：6/6，931ec10c，宿主目录实际包含四个包子场景及 MvcBind 第六场景，进入/返回、连续十轮、语言/身份、按钮/拖拽和窗口尺寸通过。
- TestAssemblyBoundaryTests：5/5，2fd7e38b，测试目录、命名空间、Player 与热更交付边界通过。

原始结果位于 ValidationArtifacts~/unified-host-*.json。本轮检查新生成的 6000-zh-menu.png 和 6000-zh-mvc.png，中文和导航布局正常。未执行项目全量测试；此次不修改生产 C#，只补齐验证和记录。真实鼠标/触摸手感及独立 Player/IL2CPP 构建仍未验收，不以事件模拟替代。

临时 2022 Editor 重新启动曾被自动审批审查以 blocked by policy 拒绝；未绕过，该 Editor 随后自行恢复调度并完成刷新。两个临时 Editor 已确认正常关闭。临时宿主目录保存至验收全部完成后清理，不包含在包或宿主交付中。未读取付费插件源码，未修改 fishinggameplay、正式 Hub、宿主 Git 依赖或 Build Settings，保留原有未提交工作。
