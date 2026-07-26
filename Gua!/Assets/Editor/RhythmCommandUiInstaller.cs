using System.IO;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    public static class RhythmCommandUiInstaller
    {
        private const string GameScenePath = "Assets/Scenes/游戏界面.unity";

        [MenuItem("Tools/Frog Camp/Add Rhythm Command Track")]
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

            controller.RefreshAudioAssetsForEditor();
            controller.BuildRhythmTrackLayoutForEditor(map);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(returnPath) && File.Exists(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("顶部音游命令轨道与新版音效引用已保存到游戏 Scene。");
        }
    }
}
