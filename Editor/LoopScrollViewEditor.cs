using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SleepyStudios.LoopScroll.Editor
{
    [CustomEditor(typeof(LoopScrollView))]
    public sealed class LoopScrollViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            try { ((LoopScrollView)target).ValidateConfiguration(); }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
            EditorGUILayout.HelpBox("Content 由插件绝对定位。动态尺寸布局组件应位于 Cell 内部。异步写入前检查 context.IsCurrent。", MessageType.Info);
        }

        [MenuItem("GameObject/UI/Sleepy Loop Scroll", false, 2060)]
        private static void CreateMenu(MenuCommand command)
        {
            var parent = command.context as GameObject;
            var root = CreateHierarchy(parent != null ? parent.transform : null);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Sleepy Loop Scroll");
            Selection.activeGameObject = root.gameObject;
        }

        /// <summary>创建可运行的原生 ScrollRect 层级及隐藏 Cell 模板，不修改已有对象。</summary>
        /// <param name="parent">UI 父节点，允许为空。</param>
        /// <returns>配置完成但尚无数据的列表。</returns>
        public static LoopScrollView CreateHierarchy(Transform parent)
        {
            var root = new GameObject("SleepyLoopScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = new Color(.055f, .075f, .12f);
            var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(400, 300);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(root.transform, false); viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(.09f, .13f, .2f);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false); content.anchorMin = content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 1);
            var template = new GameObject("CellTemplate", typeof(RectTransform), typeof(Image), typeof(LoopCell));
            template.transform.SetParent(root.transform, false); template.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 48); template.SetActive(false);
            template.GetComponent<Image>().color = new Color(.12f, .21f, .32f);
            var scroll = root.GetComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            var list = root.AddComponent<LoopScrollView>();
            list.Configure(scroll, new[] { new LoopCellPrefab { Type = 0, Prefab = template.GetComponent<LoopCell>(), Prewarm = 16 } }, LoopLayout.Vertical, new Vector2(400, 48));
            return list;
        }
    }
}
