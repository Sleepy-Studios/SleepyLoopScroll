# v0.1.0 Release Checklist

开发版本；本轮实现与验证完成后，用户已授权本地 Git 提交。本轮不推送或发布，历史推送也不代表 v0.1.0 tag 已发布。首版历史结果见 Validation.md；当前收口验证见 UnifiedValidation.md，历史勾选不代表当前代码通过。

- [x] Unity 2022.3.62f3 导入与编译。
- [x] Unity 6000.3.15f1 导入与编译。
- [x] 包 EditMode/PlayMode 十三组场景及异步隔离通过。
- [x] 60 秒同步基准内部分配为 0，预热后实例数不增加。
- [x] 锚点、定位误差在 1 Canvas UI 像素内。
- [x] 四个独立 Samples 双版本运行与截图验收，范围见 Validation。
- [x] SleepyDemos MvcBind 生成、订阅、ItemView 复用与点击身份验收。
- [x] 文档、CHANGELOG、Unity .meta 齐全，无宿主运行时依赖。
- [x] 包 API 收口、纯包 Showcase 和语言完成双版本 Test Runner 验证，见 UnifiedValidation.md。
- [x] 收口后的宿主三种正式回调、MvcBind 生成与示例 Test Runner/截图验收完成，18/18。
- [x] 定位结果/取消/偏移：双版本包 PlayMode 各 40/40、EditMode 各 2/2，60 秒基准 0 B 且实例不增加；宿主示例 7/7，见 UnifiedValidation。
- [x] 临时兼容 Editor 已关闭，本轮新增临时启动脚本已清理。
- [ ] 临时兼容宿主目录清理；保留原有工作，遵守此前删除审批限制。
- [ ] 用户明确授权发布。
- [x] 创建私有远端并提交推送开发分支。
- [ ] 打 v0.1.0 发布标签。
- [ ] 宿主由本地包切换至 Git URL/tag，并再次确认导入。

未完成门槛不得发布。私有阶段不添加开源许可证；公开前另行确定。
