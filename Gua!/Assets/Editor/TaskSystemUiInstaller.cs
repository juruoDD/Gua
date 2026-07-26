using System.IO;
using FrogCamp.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    public static class TaskSystemUiInstaller
    {
        private const string GameScenePath = "Assets/Scenes/游戏界面.unity";

        [MenuItem("Tools/Frog Camp/Rebuild Task System UI")]
        public static void RebuildWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog("重建任务系统 UI",
                    "这会替换游戏场景中的 TaskPanel 和 ReedTaskArea，" +
                    "这些对象上的手动调整会丢失。是否继续？",
                    "重建", "取消"))
                return;
            Install();
        }

        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play 模式后再重建任务系统 UI。");
                return;
            }

            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene gameScene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);

            RectTransform map = GameObject.Find("CampMap")?.GetComponent<RectTransform>();
            if (map == null)
                throw new System.InvalidOperationException("游戏界面缺少 CampMap。");

            GameObject systemObject = GameObject.Find("TaskSystem");
            if (systemObject == null)
            {
                systemObject = new GameObject("TaskSystem");
                GameObject editableUi = GameObject.Find("EditableUI");
                if (editableUi != null)
                    systemObject.transform.SetParent(editableUi.transform, false);
            }

            TaskPanelController controller =
                systemObject.GetComponent<TaskPanelController>();
            if (controller == null)
                controller = systemObject.AddComponent<TaskPanelController>();

            controller.BuildLayoutForEditor(map);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(returnPath) &&
                returnPath != GameScenePath && File.Exists(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);

            Debug.Log("任务面板、芦苇、鸟窝和私房柜判定框已保存进游戏 Scene，可在 Hierarchy 中直接调整。");
        }
    }
}
