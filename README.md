# Sleepy Loop Scroll

独立 uGUI 虚拟列表。Unity 2022.3+，运行时只依赖 `com.unity.ugui`。首版包括纵向/横向列表、固定 Grid、多 Prefab、动态尺寸、聊天、循环轮播、分页触发、选择和嵌套拖动。

## 安装

Package Manager → Add package from git URL，输入 `https://github.com/Sleepy-Studios/SleepyLoopScroll.git#v0.1.0`。宿主项目使用发布标签固定版本，并提交 `Packages/packages-lock.json`；lock 中的提交哈希由 Unity 记录，保证团队拉取同一份代码。

仓库归属 `Sleepy-Studios`，与 SleepyDemos 一样公开；安装和拉取无需 GitHub 登录或额外权限。仅修改包源码时，才将宿主依赖临时切换到仓库根目录 `package.json` 的本地包；日常协作使用远端包。后续版本新增对应的 `v` 标签，已发布标签保持不变。

## 五分钟开始

1. 在 Canvas 下使用 `GameObject → UI → Sleepy Loop Scroll`。
2. 在隐藏 `CellTemplate` 上添加业务组件，保持根节点 `LoopCell`。
3. Inspector 配置类型 0 的 Prefab、Cell 尺寸和 Prewarm；Content 不挂 LayoutGroup/ContentSizeFitter。
4. 业务先注册绑定，再提交 List；无需为十万条数据创建十万个对象。

```csharp
using SleepyStudios.LoopScroll;
using UnityEngine;
using UnityEngine.UI;

// 先注册一次，再提交调用方维护的 List；Prefab 包含 Text 和 LoopCell。
list.RegisterCellBinding<Text>((cell, index, context) => cell.text = items[index].Title);
list.SetTotalCount(items, getItemKey: item => ((ItemData)item).Id.ToString());
list.RefreshCells(); // 重新绑定活跃项
list.RefillCells(new RefillOptions(20, ScrollAlignment.Center)); // 完整重填并定位
list.ScrollToCell(20, ScrollAlignment.Center, new ScrollAnimation(.2f), offsetPixels: 40,
    onFinished: result => Debug.Log($"{result.Status}: {result.CancelReason}"));
list.CancelAnimation(); // 当前定位或等待请求只报告一次 Canceled/ExplicitCancel
```

`SetTotalCount` 接受真实 IList，null 清空；每次完整重填，默认起点。Key 使用唯一稳定业务 ID，不能依赖显示语言。集合修改后用 ApplyChanges、Append/Prepend 或 RefillCells 通知列表；RefreshCells 不更新数量。多类型、动态估算通过 SetDataSource 提交。

定位偏移为 Canvas UI 像素，最终位置 = 对齐位置 - offsetPixels，再钳制。正值向下/右，默认 0 保持现有行为。完成回调等待当前布局稳定；取消原因包括替代、拖动、手动取消、数据更新、禁用、销毁及零尺寸。inactive/零尺寸提交暂存原请求，恢复后执行。IsAnimating 仅表示插值动画状态；完整契约见 API Reference。

## 示例与文档

Package Manager → Samples → 导入 **Loop Scroll Showcase**，打开导入目录的 `Main.unity` 即可。主菜单进入基础、多类型、聊天、轮播分页四个真实子场景，每个页面可返回；子场景也能直接运行。首次中文，主菜单 `中文 / English` 切换并记住选择。

Editor 菜单 `Tools/Sleepy Loop Scroll/Open Showcase` 打开主页；`Build Imported Sample Scenes` 重建完整场景组。重建前保存当前场景。导航自动解析导入目录，不需要把示例加入宿主 Build Settings。独立 Player 演示构建需显式包含 Main、Basic、MultiType、Chat、CarouselPaging 五个场景；本包不修改宿主构建配置。

示例中文字库 Noto Sans SC Regular 及字体许可证放在 Showcase/Common/Resources/SleepyLoopScrollSamples；字体授权仅覆盖该字库，不覆盖本包代码。

- [Manual](Documentation~/Manual.md)：层级、Inspector、固定与动态尺寸。
- [API Reference](Documentation~/APIReference.md)：数据更新、锚点与定位。
- [Recipes](Documentation~/Recipes.md)：背包、Chat、Carousel、Paging、Nested Scroll。
- [Migration](Documentation~/Migration.md) 与 [Troubleshooting](Documentation~/Troubleshooting.md)。
- [当前验收记录](Documentation~/UnifiedValidation.md)。
- [Architecture](Documentation~/Architecture.md)、[Performance](Documentation~/Performance.md)、[Release Checklist](Documentation~/ReleaseChecklist.md)。

私有阶段不授予开源许可。实现独立设计，不包含付费插件源码。
