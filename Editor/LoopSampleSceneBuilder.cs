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
            Build("Basic", "BasicLoopSample"); Build("MultiType", "MultiTypeLoopSample");
            Build("Chat", "ChatLoopSample"); Build("CarouselPaging", "CarouselPagingLoopSample");
            AssetDatabase.Refresh();
        }
        private static void Build(string folder, string className)
        {
            var guids = AssetDatabase.FindAssets(className + " t:MonoScript", new[] { "Assets" });
            MonoScript script = null;
            foreach (var guid in guids)
            {
                var candidate = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != null && candidate.GetClass() != null && candidate.GetClass().Name == className) { script = candidate; break; }
            }
            if (script == null) throw new InvalidOperationException("请先导入示例 " + className);
            var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script)).Replace('\\', '/') + "/" + folder + ".unity";
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var root = new GameObject(className, typeof(RectTransform)); SceneManager.MoveGameObjectToScene(root, scene); root.AddComponent(script.GetClass());
                var cameraObject = new GameObject("Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; cameraObject.GetComponent<Camera>().backgroundColor = Color.black;
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("示例场景保存失败：" + path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
        }
    }
}
