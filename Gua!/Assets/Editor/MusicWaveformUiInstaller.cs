using System.IO;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    public static class MusicWaveformUiInstaller
    {
        private const string GameScenePath = "Assets/Scenes/游戏界面.unity";

        [MenuItem("Tools/Frog Camp/Add Music Waveform UI")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene gameScene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);
            GameSceneController controller =
                Object.FindObjectOfType<GameSceneController>();
            RectTransform map =
                GameObject.Find("CampMap")?.GetComponent<RectTransform>();
            if (controller == null || map == null)
                throw new System.InvalidOperationException(
                    "游戏界面缺少控制器或 CampMap。");

            controller.BuildWaveformLayoutForEditor(map);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(returnPath) && File.Exists(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("实时音乐波形 UI 已添加到游戏 Scene。");
        }
    }
}
