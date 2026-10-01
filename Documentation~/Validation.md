# Validation

日期：2026-10-01（Asia/Shanghai）。状态：本地实现与双版本功能/性能验证完成；开发代码提交至私有仓库 `Sleepy-Studios/SleepyLoopScroll` 的 `codex/v0.1-implementation` 分支，尚未发布 `v0.1.0` 标签。用户已授权代码提交与推送；宿主继续使用本地包依赖。临时宿主目录的删除被自动审批拒绝，目录清理仍需用户完成。

| 验证范围 | Unity 2022.3.62f3 / uGUI 1.0.0 | Unity 6000.3.15f1 / uGUI 2.0.0 |
|---|---|---|
| 导入与 Editor 编译 | 通过，临时独立宿主 | 通过，SleepyDemos |
| Fenwick EditMode 算法测试 | 2/2 | 2/2 |
| 包 PlayMode 功能覆盖 | 24 个方法全部通过，包括单项补验 | 24 个方法全部通过，包括单项补验 |
| 60 秒原生 GC.Alloc 基准 | 8,612 次定位、0 B、50 个实例 | 8,629 次定位、0 B、50 个实例 |
| 首次/重复获取 Token | 200 B / 0 B | 200 B / 0 B |
| 四个包 Samples 运行、按钮操作、截图 | 4/4，截图已检查 | 4/4，截图已检查 |

宿主额外验证：MvcBind 注册发现与生成 2/2；ItemView 复用、订阅去重/解除、嵌套 ItemView、inactive 初始化 4/4；程序集边界 5/5；四个包示例及宿主 MvcBind 示例操作验收合计 5/5。

## 结果口径

主功能测试先按整个包类执行。2022.3 的初次完整运行有一个 CTS 分配测量失败：系统 GC.GetAllocatedBytesForCurrentThread 返回 0。随后改用已知分配校准的原生 ProfilerRecorder，两版本分别补验该异步用例和 60 秒基准；动态目标 Reload 对齐作为第 24 个方法单独补验。表中统计最终有效方法覆盖，不把中间失败记录或旧计数器的零值算作通过。

基准只在主线程同步 ScrollTo/Reconcile 调用区间启用 recorder；Test Runner、帧间 Editor、Unity 外部更新不计入此区间。预热后 CreatedCellCount 始终为 50。固定配置下无滚动 Instantiate；Viewport 扩大等新增容量需求另计。锚点/对齐测试使用 1 Canvas UI 像素容差。

运行操作包括基础模式切换与第 50,000 项定位、多类型选择/清除、聊天历史前插/未读/回到底部、轮播翻页与分页新增；截图检查文字可见性、列表裁剪与布局。真实鼠标/触摸手感、Player/IL2CPP 构建未执行，不把自动事件模拟或 Editor 验证描述为这些项目通过。

## 证据与清理

原始 JSON、日志和双版本截图保存在本地 `ValidationArtifacts~/`，该目录被 Unity 和 Git 忽略，不进入包发布。包的示例场景使用 2022.3 生成的版本。

SleepyDemos 内的本次接入安装脚本、临时示例验收脚本及 `.meta` 已删除；长期回归仍保留在包 Tests 与宿主原有两个测试程序集。临时 2022.3 Editor 已正常关闭，目录 `D:/Unity/Unity_Project/SleepyLoopScrollValidation2022` 的递归/逐项删除均被自动审批拒绝，需要用户手动清理。已有用户改动保持不变，未修改 fishinggameplay 业务代码。

测试由 Unity Test Runner 运行，限定包测试、宿主桥接和直接受影响的已有测试。未运行无关项目全量测试。不通过 dotnet/msbuild 验证 Unity 工程。
