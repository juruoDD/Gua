using FrogCamp.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    [InitializeOnLoad]
    public static class TutorialPresentationInstaller
    {
        private const string SessionKey =
            "FrogCamp.TutorialPresentationInstallerV4";

        static TutorialPresentationInstaller()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += BakeActiveGameScene;
        }

        [MenuItem("Tools/Frog Camp/Bake Tutorial Presentation UI")]
        public static void BakeActiveGameScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "游戏界面") return;

            GameSceneController controller =
                Object.FindObjectOfType<GameSceneController>();
            if (controller == null) return;

            controller.BakeTutorialPresentationForEditor();
            UpgradeTutorialContent(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "新手教程 UI 已烘焙到 Canvas：TutorialRulesOverlay / TrialTimer");
        }

        private static void UpgradeTutorialContent(
            GameSceneController controller)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/DingTalk JinBuTi Dynamic SDF.asset");
            TextMeshProUGUI[] texts =
                Object.FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name != "RulesTitle" &&
                    text.name != "RulesBody" &&
                    text.name != "RulesFooter")
                    continue;
                ApplyFont(text, font);
                if (text.name == "RulesBody")
                    text.text = text.text.Replace(
                        "25 秒", "30 秒").Replace("25秒", "30秒");
                else if (text.name == "RulesFooter")
                    text.text = "规则说明将在 {0} 秒后自动关闭";
                EditorUtility.SetDirty(text);
            }

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty bodyProperty =
                serialized.FindProperty("tutorialRulesBody");
            if (bodyProperty != null)
                bodyProperty.stringValue = bodyProperty.stringValue
                    .Replace("25 秒", "30 秒")
                    .Replace("25秒", "30秒");
            SerializedProperty footerProperty =
                serialized.FindProperty("tutorialRulesFooter");
            if (footerProperty != null)
                footerProperty.stringValue =
                    "规则说明将在 {0} 秒后自动关闭";
            SerializedProperty introProperty =
                serialized.FindProperty("trialIntroText");
            if (introProperty != null)
                introProperty.stringValue = "30秒试玩";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyFont(
            TextMeshProUGUI text, TMP_FontAsset font)
        {
            if (text != null && font != null)
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }
        }
    }
}
