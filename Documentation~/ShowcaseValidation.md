# Showcase 与 API 调整验证

日期：2026-10-01（Asia/Shanghai）。本页记录示例调整阶段的历史状态；当前改动随开发提交维护，尚未打发布标签，宿主仍使用本地包。首次版本基线见 Validation.md，本页保留示例调整阶段的历史结果；当前正式 API 和最新验证见 [UnifiedValidation](UnifiedValidation.md)。旧方法和六种回调已删除，下列历史结果不代表当前收口代码通过。

## 收口前的历史验收

- 一个 Showcase 导入入口、Main 加四个可返回的独立场景；宿主场景目录额外包含 MvcBind 入口。
- 默认中文、持久化语言选择、稳定 ID、中文字库与字体许可。示例代码/场景迁移保留已有 GUID。
- RegisterCellBinding/SetTotalCount 与四种常用操作别名；Core 的 ItemView 工厂、旧风格注册与 MvcBind 六种回调约定已实现。
- Unity 2022.3.62f3 导入、正式 Editor 编译与包回归 26/26（job 2432bd3c）。随后补测新增注册模式 60 秒基准 1/1（2f25643b），覆盖最终 27 个包 PlayMode 方法。
- 2022.3 原 SetData 模式与注册模式的 60 秒基准均为 0 B，实例数保持 50；注册模式 8,639 次定位。测量口径与原 Performance.md 相同，业务与帧间 Editor 分配不计入调用区间。
- 2022.3 独立包示例回归 5/5（77adbe1d），包含全入口往返、连续十轮、语言与身份、直接子场景返回、按钮与原生拖拽事件。缺失场景恢复补验 1/1（ad9ac3a2）。
- 2022.3 另覆盖 1280×720、720×1280、1920×1080 三种 Game View 尺寸，检查按钮边界、文本完整性及截图（83cacef0）；测试结束恢复原尺寸并移除本次临时配置。竖屏背景填充调整后补验 1/1（967127c7），截图已检查，最终共覆盖六个示例测试方法。

原始 JSON 与截图位于本地忽略目录 ValidationArtifacts~/showcase；部分基准补验第一次遇到 UnitySkills batch_state.json 共享冲突，重新刷新并发现精确方法后通过。不把那次工具回调重连失败统计为通过。

## 当时尚未完成的验收（宿主已于后续补验恢复）

SleepyDemos 的 Unity 6000.3.15f1 Editor 主线程长时间不调度 UnitySkills 队列。后台 health 可访问，但主线程请求超时；UPM 日志出现未登录导致元数据下载失败的记录。仅恢复其包管理服务未能解除停顿，已请求用户检查 Editor 提示；未退出 Play Mode或重启该 Editor。

因此本次最终 Unity 6 编译、包回归、宿主桥接/生成/程序集边界测试及六个页面运行验收仍待完成，不能沿用首次实现结果声称本次通过。该阻塞已在后续重试解除，正式宿主编译和 18/18 相关测试见 UnifiedValidation；下述内容保留阶段历史，不作为当前阻塞。

宿主 MvcBind 场景由 2022.3 的原生场景保存 API 生成，使用相同脚本 GUID 和基类序列化字段的临时绑定桩。这里只验证场景资源结构，不作为 Hotfix/Core 桥接功能通过的证据；桩不进入包或宿主交付。

按钮和拖拽使用 Unity Test Runner 的真实 UnityEvent/原生事件处理入口模拟；真实鼠标/触摸手感与 Player/IL2CPP 构建尚未验收。上述未完成项阻止发布。

## 清理与边界

未改 fishinggameplay，未改宿主 Git 依赖或 Build Settings，没有发布标签。保留用户原有 UnitySkills 配置与 vTabs 改动。临时 2022.3 Editor 已正常关闭，场景保存用的绑定桩和临时 Builder 已删除，目录保留验证记录；完成全部验收后按原发布门槛清理。
