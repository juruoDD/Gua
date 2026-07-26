using System.IO;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    public static class GameSettingsUiInstaller
    {
        private const string GameScenePath = "Assets/Scenes/游戏界面.unity";

        [MenuItem("Tools/Frog Camp/Recreate Game Settings UI (Overwrite)")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play 模式后再安装设置面板。");
                return;
            }
            if (!EditorUtility.DisplayDialog("覆盖设置界面",
                    "这会删除并重新创建游戏 Scene 中的 SettingsButton 和 SettingsPanel，" +
                    "你对这两个对象的手动调整会丢失。是否继续？",
                    "覆盖重建", "取消"))
                return;

            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene gameScene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);
            GameSceneController controller =
                Object.FindObjectOfType<GameSceneController>();
            if (controller == null)
                throw new System.InvalidOperationException("游戏界面缺少 GameSceneController。");

            RectTransform map = GameObject.Find("CampMap")?.GetComponent<RectTransform>();
            if (map == null)
                throw new System.InvalidOperationException("游戏界面缺少 CampMap。");

            controller.BuildSettingsLayoutForEditor(map);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(returnPath) && File.Exists(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);

            Debug.Log("游戏设置按钮和音量面板已保存到 Scene，可在 Hierarchy 中手动编辑。");
        }
    }
}
