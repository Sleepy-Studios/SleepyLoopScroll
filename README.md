# Sleepy Loop Scroll

独立 uGUI 虚拟列表。Unity 2022.3+，运行时只依赖 `com.unity.ugui`。首版包括纵向/横向列表、固定 Grid、多 Prefab、动态尺寸、聊天、循环轮播、分页触发、选择和嵌套拖动。

## 安装

开发：Package Manager → Add package from disk，选择仓库根目录 `package.json`。仓库应放在宿主之外，由本地 Git 独立维护。

发布后：Package Manager → Add package from git URL，使用私有仓库地址 `https://github.com/Sleepy-Studios/SleepyLoopScroll.git#v0.1.0`。需要本机 Git 对私有仓库有访问权限。当前远端/tag 尚未发布，不应把此地址当作已可安装版本。

## 五分钟开始

1. 在 Canvas 下使用 `GameObject → UI → Sleepy Loop Scroll`。
2. 在隐藏 `CellTemplate` 上添加业务组件，保持根节点 `LoopCell`。
3. Inspector 配置类型 0 的 Prefab、Cell 尺寸和 Prewarm；Content 不挂 LayoutGroup/ContentSizeFitter。
4. 业务提交 List 与绑定委托；无需为十万条数据创建十万个对象。

```csharp
using SleepyStudios.LoopScroll;
using UnityEngine.UI;

// Prefab 根节点包含 Image 和 LoopCell；业务自行拥有 items。
list.SetData<string, Image>(items,
    (cell, item, context) => cell.color = UnityEngine.Color.white,
    unbind: null,
    getItemKey: item => item);

items.Insert(0, "history:42");
list.Prepend(1); // 稳定 Key + 像素锚点
list.ScrollTo(20, ScrollAlignment.Center, new ScrollAnimation(.2f));
```

Key 必须唯一。真实业务使用 ID，显示文本可以重复；上述字符串仅作为最小示例。集合修改后必须通知列表。

## 示例与文档

Package Manager Samples 导入后直接打开同目录的场景：Basic、MultiType、Chat、CarouselPaging。每个 Sample 独立，无宿主依赖。

- [Manual](Documentation~/Manual.md)：层级、Inspector、固定与动态尺寸。
- [API Reference](Documentation~/APIReference.md)：数据更新、锚点与定位。
- [Recipes](Documentation~/Recipes.md)：背包、Chat、Carousel、Paging、Nested Scroll。
- [Migration](Documentation~/Migration.md) 与 [Troubleshooting](Documentation~/Troubleshooting.md)。
- [Architecture](Documentation~/Architecture.md)、[Performance](Documentation~/Performance.md)、[Release Checklist](Documentation~/ReleaseChecklist.md)。

私有阶段不授予开源许可。实现独立设计，不包含付费插件源码。
