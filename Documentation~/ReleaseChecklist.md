# v0.1.0 Release Checklist

开发版本；代码提交与推送已获授权，不代表 v0.1.0 tag 已发布。验证结果见 Validation.md。

- [x] Unity 2022.3.62f3 导入与编译。
- [x] Unity 6000.3.15f1 导入与编译。
- [x] 包 EditMode/PlayMode 十三组场景及异步隔离通过。
- [x] 60 秒同步基准内部分配为 0，预热后实例数不增加。
- [x] 锚点、定位误差在 1 Canvas UI 像素内。
- [x] 四个独立 Samples 双版本运行与截图验收，范围见 Validation。
- [x] SleepyDemos MvcBind 生成、订阅、ItemView 复用与点击身份验收。
- [x] 文档、CHANGELOG、Unity .meta 齐全，无宿主运行时依赖。
- [ ] 临时兼容宿主关闭并清理，原有工作保留。
- [ ] 用户明确授权发布。
- [x] 创建私有远端并提交推送开发分支。
- [ ] 打 v0.1.0 发布标签。
- [ ] 宿主由本地包切换至 Git URL/tag，并再次确认导入。

未完成门槛不得发布。私有阶段不添加开源许可证；公开前另行确定。
