# Loop Scroll 验收记录

## 定位请求完成、取消与像素偏移（2026-10-01）

本轮在包基线 2a048b2 / codex/v0.1-implementation 和宿主 011bd9e / main 上完成实现与验证；用户随后授权本地 Git 提交。直接扩展 ScrollToCell 的 offsetPixels/onFinished 和 ScrollToOffset 的 onFinished，新增只读 ScrollResult；没有兼容重载、Task/CTS 请求包装或发布操作。

### 编译与 Test Runner

| 直接相关范围 | Unity 2022.3.62f3 / uGUI 1.0.0 | Unity 6000.3.15f1 / uGUI 2.0.0 |
|---|---|---|
| 最终包、测试与示例程序集正式编译 | 通过，15:00:45 UTC | 通过，15:00:45 UTC |
| LoopScrollPlayModeTests | 40/40，56fc2377 | 40/40，b52ceacd |
| LoopScrollAlgorithmTests | 2/2，e6e9b40e | 2/2，de339bc7 |
| SleepyDemos Core/Hotfix 与宿主测试正式编译 | 宿主仅为 Unity 6 | 通过，15:00:46 UTC |
| 宿主 LoopScrollShowcaseTests | 不适用 | 7/7，2f6771b2 |

包新增 14 项永久回归，固定矩阵覆盖纵横 List、纵横固定 Grid、三种对齐、正负偏移和立即/平滑共 48 组组合，并检查首尾钳制。动态矩阵覆盖纵横 List、三种对齐、正负偏移共 12 组，检查测量、刷新、Viewport 调整及完成后异步尺寸变化不重复通知。所有位置断言容差为 1 Canvas UI 像素。

生命周期用例覆盖替代、手动取消、原生拖动接管、成功提交/重填/结构变更、失败数据验证、禁用、销毁、任一轴零尺寸、inactive/零尺寸恢复和原动画时长。断言通知恰好一次、无效输入不影响旧请求、完成前实测尺寸修正、IsAnimating=false 时仍可取消布局收敛，以及恢复后立即定位不被旧重填锚点覆盖。

通知重入覆盖完成/取消回调内再次定位、重填、回调异常后的继续定位、绑定保护退出后的通知及点击回调内原有数据更新能力。Carousel 的偏移完成、拖动取消、吸附、循环回正和既有高速/反向拖动用例通过；Chat/Selection/入场行为未新增功能。

首轮 Unity 6 的 CellCancellationNotificationRunsAfterCellCallbackGuards 测试因重新注册绑定却未重新提交数据源而失败（37/38）；修正测试设置后精确补验 1/1，再以最终代码完整运行上述 40/40。临时宿主初次类筛选使用旧缓存，只运行 26 项，因此该结果不计入本轮最终验收；最终重新发现并确认 40 项后执行。原始失败和补验 JSON 保留在忽略目录。

### 性能

重跑原有注册路径、100000 项、预热 50 实例的 60 秒同步定位基准。原生 GC.Alloc recorder 只测同步 ScrollToCell/Reconcile 当前线程区间，不混入 Unity/Editor 帧间、业务异步或 Token 开销。

| 最终版本 | 调用次数 | 内部分配 | 创建实例 |
|---|---:|---:|---:|
| 2022.3.62f3 | 8588 | 0 B | 50，未增加 |
| 6000.3.15f1 | 8631 | 0 B | 50，未增加 |

无回调定位以值字段保存请求，不创建逐次闭包、请求对象、Task 或 CTS；通知记录使用复用容器。包 Console 的两条 Error 为已有缺失组件测试和新回调异常恢复测试的预期日志，均以 LogAssert 验证，无非预期错误。

### 宿主交付、证据与限制

Hotfix 的 MvcBind 页面新增 ±60 偏移、取消和中英完成/取消状态，按钮保持可用，拖动打断也更新状态。Core.Runtime/Core.Editor 桥接未改；宿主共享样例翻译与包源一致，所有场景、Prefab、脚本 .meta/GUID 保留。

宿主最终 Console Error 为 0；原生 TestResults.xml 确认 7/7，已复制至忽略目录。已检查最终中文拖动取消和英文负偏移完成截图，新增按钮、文字和间距正常；正偏移完成截图也已生成。

APIReference、README、CHANGELOG、Architecture、此记录和宿主 loop-scroll 模块/接入/Demo 入口说明已同步。原始结果、最终编译 JSON、Editor 日志和截图位于 ValidationArtifacts~/requests-* 与 showcase/，不提交原始产物。

未执行宿主全量测试、无关测试或 Player/IL2CPP 构建；未使用 dotnet/msbuild 或同项目 BatchMode，未检查 Hot Reload，也未重启真实宿主。交互为 Test Runner 的按钮/拖动事件模拟，截图检查不代表真实鼠标/触摸手感验收。

两个临时 Editor 已正常关闭；临时兼容宿主目录保留，避免绕过此前目录删除审批拒绝。本轮新增的临时启动脚本及 .meta 已清理，未删除此前已有验证脚本。宿主原有 agent_config.json 与 vTabs StyleSheets 未提交改动保留。不修改 fishinggameplay、Hub、Build Settings 或宿主 Git 依赖，按用户后续授权进行本地 Git 提交，不推送、打标签或发布。

## 前轮 API 收口历史

以下为前轮收口的历史记录，不能替代上面的最终定位请求验收。

日期：2026-10-01。代码已在本地统一，尚未发布；2026-10-01 22:17 后真实 Unity 6 宿主恢复响应，编译和直接相关自动化验收已补齐。真实触摸/Player 构建、临时目录清理与发布授权仍按检查表执行。初版历史见 Validation，示例调整阶段历史见 ShowcaseValidation；当前正式 API 以 APIReference 为准。

### 实现范围

- 包 Runtime 只保留 RegisterCellBinding、SetTotalCount、SetDataSource、RefreshCells、RefillCells(RefillOptions)、ScrollToCell 及集合变更、动态尺寸与偏移定位能力。没有旧名转发、过渡重载或 Obsolete。
- 简单集合只使用 IList 的 ListDataSource，删除 RegisteredListDataSource。提交保存绑定配置，后续注册不改变活跃项的原解绑回调。
- Core.Runtime 只保留 RegisterLoopScrollRect/Click/ItemHide；绑定/点击为 (ItemView,index,CellBindContext)，解绑为 (ItemView,CellBindContext)。仅泛型绑定入口指定工厂；View 和嵌套 ItemView 复用同一订阅逻辑。
- 删除 SetItems、平行 ItemBound/ItemHidden/ItemClicked、LegacyAdapter 和委托转换缓存。MvcBind 仅三种正式约定。
- 迁移包扩展、四个子场景、宿主示例和相关测试。已有示例脚本/场景 GUID 对照通过，宿主导入脚本与包一致。两套性能基准合并为注册路径的一套。

### 编译与自动化

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

### 性能

一次注册路径、固定同步绑定、不读取 Token，预热 50 个实例后测试 100000 项连续 60 秒。原生 GC.Alloc recorder 只收集同步 ScrollToCell/Reconcile 调用区间：

| 版本 | 定位次数 | 分配 | 实例数 |
|---|---:|---:|---:|
| 2022.3 | 8622 | 0 B | 50，未增加 |
| 6000.3 | 8641 | 0 B | 50，未增加 |

异步 Token、业务/TMP、帧间 Unity/Editor 开销不混入该口径。锚点、动态定位与异步过期隔离包含在上述包回归中，位置断言容差为 1 UI 像素。

### 证据和待办

原始 JSON、Editor 日志及截图位于忽略目录 ValidationArtifacts~，文件名前缀 unified-2022 / unified-6000。截图位于 showcase 子目录；本轮已检查 2022 中文主菜单/竖屏以及 Unity 6 中文聊天/轮播截图，文字显示正常。

此前 SleepyDemos 主线程停滞，本轮重试已恢复；未重启真实宿主。通过 Assets/Refresh 确认最新代码正式编译后，串行运行以下 Unity Test Runner 类，全部 18/18 通过：

- LoopScrollMvcGenerationTests：2/2，7b16551f，仅三种带 context 的正式回调发现/生成。
- LoopScrollItemViewBridgeTests：5/5，067a8289，工厂、注册去重、ItemView 复用、解绑/点击身份、过期隔离、嵌套订阅释放和 inactive 恢复。
- LoopScrollShowcaseTests：6/6，931ec10c，宿主目录实际包含四个包子场景及 MvcBind 第六场景，进入/返回、连续十轮、语言/身份、按钮/拖拽和窗口尺寸通过。
- TestAssemblyBoundaryTests：5/5，2fd7e38b，测试目录、命名空间、Player 与热更交付边界通过。

原始结果位于 ValidationArtifacts~/unified-host-*.json。本轮检查新生成的 6000-zh-menu.png 和 6000-zh-mvc.png，中文和导航布局正常。未执行项目全量测试；此次不修改生产 C#，只补齐验证和记录。真实鼠标/触摸手感及独立 Player/IL2CPP 构建仍未验收，不以事件模拟替代。

临时 2022 Editor 重新启动曾被自动审批审查以 blocked by policy 拒绝；未绕过，该 Editor 随后自行恢复调度并完成刷新。两个临时 Editor 已确认正常关闭。临时宿主目录保存至验收全部完成后清理，不包含在包或宿主交付中。未读取付费插件源码，未修改 fishinggameplay、正式 Hub、宿主 Git 依赖或 Build Settings，保留原有未提交工作。
