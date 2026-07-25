using System.IO;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    [InitializeOnLoad]
    public static class CampSceneBaker
    {
        private const string InitialPath = "Assets/Scenes/初始界面.unity";
        private const string StartPath = "Assets/Scenes/开始界面.unity";
        private const string LobbyPath = "Assets/Scenes/联机界面.unity";
        private const string GamePath = "Assets/Scenes/游戏界面.unity";
        private const string SessionKey = "FrogCamp.EditableUiBakeV5";

        static CampSceneBaker()
        {
            EditorApplication.delayCall += ConfigurePlayModeStartScene;
            if (AllScenesAreBaked() || SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += BakeScenes;
        }

        [MenuItem("Tools/Frog Camp/Rebuild Editable UI Scenes")]
        public static void RebuildScenes()
        {
            if (!EditorUtility.DisplayDialog("重建可编辑界面",
                    "这会重新生成四个 Scene 中名为 EditableUI 的界面对象，其他对象会保留。是否继续？",
                    "重建", "取消")) return;
            BakeScenes();
        }

        private static void BakeScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play 模式后再重建界面。");
                return;
            }

            string returnScenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(returnScenePath) ||
                !returnScenePath.StartsWith("Assets/Scenes/"))
                returnScenePath = InitialPath;
            EditorSceneManager.SaveOpenScenes();

            BuildInitialScene();
            BuildExistingScene<StartSceneController>(StartPath,
                controller => controller.BuildLayoutForEditor());
            BuildExistingScene<LobbySceneController>(LobbyPath,
                controller => controller.BuildLayoutForEditor());
            BuildExistingScene<GameSceneController>(GamePath,
                controller => controller.BuildLayoutForEditor());

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(InitialPath, true),
                new EditorBuildSettingsScene(StartPath, true),
                new EditorBuildSettingsScene(LobbyPath, true),
                new EditorBuildSettingsScene(GamePath, true)
            };
            ConfigurePlayModeStartScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(returnScenePath) && File.Exists(returnScenePath))
                EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            else
                EditorSceneManager.OpenScene(InitialPath, OpenSceneMode.Single);

            Debug.Log("四个可编辑 UI Scene 已生成，初始界面已设为第一个启动场景。");
        }

        private static void ConfigurePlayModeStartScene()
        {
            SceneAsset initialScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(InitialPath);
            if (initialScene != null)
                EditorSceneManager.playModeStartScene = initialScene;
        }

        private static bool AllScenesAreBaked()
        {
            return SceneFileContainsEditableUi(InitialPath) &&
                   SceneFileContainsEditableUi(StartPath) &&
                   SceneFileContainsEditableUi(LobbyPath) &&
                   SceneFileContainsEditableUi(GamePath) &&
                   File.ReadAllText(GamePath).Contains("m_Name: ActorLayer") &&
                   File.ReadAllText(GamePath).Contains("guid: 0a3be05111c82504e8f6f645c8b46bad");
        }

        private static bool SceneFileContainsEditableUi(string path)
        {
            return File.Exists(path) && File.ReadAllText(path).Contains("m_Name: EditableUI");
        }

        private static void BuildInitialScene()
        {
            Scene scene;
            if (File.Exists(InitialPath))
                scene = EditorSceneManager.OpenScene(InitialPath, OpenSceneMode.Single);
            else
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RemoveGeneratedUi(scene);
            GameObject root = new GameObject("EditableUI");
            InitialSceneController controller = root.AddComponent<InitialSceneController>();
            controller.BuildLayoutForEditor();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, InitialPath);
        }

        private static void BuildExistingScene<T>(string path, System.Action<T> build)
            where T : MonoBehaviour
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RemoveGeneratedUi(scene);
            GameObject root = new GameObject("EditableUI");
            T controller = root.AddComponent<T>();
            build(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveGeneratedUi(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "EditableUI" || root.name == "CampRuntimeUI")
                    Object.DestroyImmediate(root);
            }
        }
    }
}
