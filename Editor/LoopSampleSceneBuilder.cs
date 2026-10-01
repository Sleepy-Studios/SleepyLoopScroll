using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SleepyStudios.LoopScroll.Editor
{
    public static class LoopSampleSceneBuilder
    {
        [MenuItem("Tools/Sleepy Loop Scroll/Build Imported Sample Scenes")]
        public static void BuildImportedScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在 Edit Mode 构建示例场景。");
            var menu = FindScript("LoopSampleMenu");
            var root = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(menu))).Replace('\\', '/');
            var resourceFolder = root + "/Common/Resources/SleepyLoopScrollSamples";
            var catalogPath = resourceFolder + "/Catalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance(FindScript("LoopSampleCatalog").GetClass());
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries"); entries.arraySize = 5;
            SetEntry(entries.GetArrayElementAtIndex(0), "menu", "menu", "choose", "Main.unity");
            SetEntry(entries.GetArrayElementAtIndex(1), "basic", "basic", "basicDesc", "Basic/Basic.unity");
            SetEntry(entries.GetArrayElementAtIndex(2), "multi", "multi", "multiDesc", "MultiType/MultiType.unity");
            SetEntry(entries.GetArrayElementAtIndex(3), "chat", "chat", "chatDesc", "Chat/Chat.unity");
            SetEntry(entries.GetArrayElementAtIndex(4), "carousel", "carousel", "carouselDesc", "CarouselPaging/CarouselPaging.unity");
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
            var font = AssetDatabase.LoadAssetAtPath<Font>(resourceFolder + "/NotoSansSC-Regular.otf");
            if (font == null) throw new InvalidOperationException("请先导入完整 Showcase（包括中文字库）。");
            Build(root + "/Main.unity", menu.GetClass(), catalog, font);
            Build(root + "/Basic/Basic.unity", FindScript("BasicLoopSample").GetClass(), catalog, font);
            Build(root + "/MultiType/MultiType.unity", FindScript("MultiTypeLoopSample").GetClass(), catalog, font);
            Build(root + "/Chat/Chat.unity", FindScript("ChatLoopSample").GetClass(), catalog, font);
            Build(root + "/CarouselPaging/CarouselPaging.unity", FindScript("CarouselPagingLoopSample").GetClass(), catalog, font);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Sleepy Loop Scroll/Open Showcase")]
        public static void OpenShowcase()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在 Edit Mode 打开示例。");
            var script = FindScript("LoopSampleMenu");
            var root = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(script))).Replace('\\', '/');
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(root + "/Main.unity");
        }
        private static MonoScript FindScript(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:MonoScript", new[] { "Assets" }))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() != null && script.GetClass().Name == name) return script;
            }
            throw new InvalidOperationException("请先导入 Loop Scroll Showcase：" + name);
        }
        private static void SetEntry(SerializedProperty entry, string id, string title, string description, string path)
        {
            entry.FindPropertyRelative("id").stringValue = id; entry.FindPropertyRelative("titleKey").stringValue = title;
            entry.FindPropertyRelative("descriptionKey").stringValue = description; entry.FindPropertyRelative("scenePath").stringValue = path;
        }
        private static void Build(string path, Type driver, ScriptableObject catalog, Font font)
        {
            // 原场景可能正好是要重建的示例。先检查再临时切场景，避免覆盖未保存工作。
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (open.isDirty || string.IsNullOrEmpty(open.path))
                    throw new InvalidOperationException("请先保存当前场景或打开一个已保存的场景，再重建示例。");
            }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var root = new GameObject(driver.Name, typeof(RectTransform)); SceneManager.MoveGameObjectToScene(root, scene);
                var page = root.AddComponent(driver); driver.GetMethod("ConfigureAssets").Invoke(page, new object[] { catalog, font });
                var cameraObject = new GameObject("Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; cameraObject.GetComponent<Camera>().backgroundColor = Color.black;
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("示例场景保存失败：" + path);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
