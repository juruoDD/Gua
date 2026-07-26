using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    public static class InitialAmbientEffectsInstaller
    {
        private const string InitialScenePath = "Assets/Scenes/初始界面.unity";

        [MenuItem("Tools/Frog Camp/Add Initial Pixel Ambient Effects")]
        public static void Install()
        {
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene initialScene = returnPath == InitialScenePath
                ? SceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(InitialScenePath, OpenSceneMode.Single);

            InitialSceneController controller =
                Object.FindObjectOfType<InitialSceneController>();
            if (controller == null)
                throw new System.InvalidOperationException(
                    "初始界面缺少 InitialSceneController。");

            Canvas canvas = controller.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                throw new System.InvalidOperationException("初始界面缺少 Canvas。");

            Transform existing = canvas.transform.Find("PixelAmbientEffects");
            PixelAmbientEffects effects;
            if (existing != null)
            {
                effects = existing.GetComponent<PixelAmbientEffects>();
                if (effects == null)
                    effects = existing.gameObject.AddComponent<PixelAmbientEffects>();
            }
            else
            {
                GameObject effectObject = new GameObject(
                    "PixelAmbientEffects",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(PixelAmbientEffects));
                effectObject.transform.SetParent(canvas.transform, false);
                RectTransform rect = effectObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                effects = effectObject.GetComponent<PixelAmbientEffects>();
            }

            RectTransform title = FindRect(canvas.transform, "标题") ??
                FindRect(canvas.transform, "Title");
            effects.Configure(title);
            effects.transform.SetSiblingIndex(Mathf.Min(1,
                canvas.transform.childCount - 1));
            EditorUtility.SetDirty(effects);
            EditorSceneManager.MarkSceneDirty(initialScene);
            EditorSceneManager.SaveScene(initialScene, InitialScenePath);

            if (!string.IsNullOrEmpty(returnPath) && returnPath != InitialScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);

            Debug.Log("初始界面的像素水花与标题星光特效层已保存到 Scene。");
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            foreach (RectTransform item in
                root.GetComponentsInChildren<RectTransform>(true))
            {
                if (item.name == objectName) return item;
            }
            return null;
        }
    }
}
