using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Editor
{
    public static class SceneMotionUiInstaller
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/初始界面.unity",
            "Assets/Scenes/开始界面.unity",
            "Assets/Scenes/联机界面.unity",
            "Assets/Scenes/游戏界面.unity"
        };

        private const string StartPath = "Assets/Scenes/开始界面.unity";
        private const string LobbyPath = "Assets/Scenes/联机界面.unity";
        private const string InitialPath = "Assets/Scenes/初始界面.unity";
        private const string MenuMusicPath = "Assets/Sound/初始bgm.mp3";
        private static readonly Color TransitionBlue =
            new Color(86f / 255f, 161f / 255f, 1f, 1f);

        [MenuItem("Tools/Frog Camp/Add Start and Lobby Ambient and Scene Transitions")]
        public static void Install()
        {
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            foreach (string path in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Canvas canvas = Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                    throw new System.InvalidOperationException(path + " 缺少 Canvas。");
                if (path == StartPath) InstallStartAmbient(canvas);
                if (path == LobbyPath) InstallLobbyAmbient(canvas);
                InstallTransition(canvas);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, path);
            }
            if (!string.IsNullOrEmpty(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("开始、联机界面水景特效与四个 Scene 的过渡层已保存。");
        }

        [MenuItem("Tools/Frog Camp/Add Start Ambient Only")]
        public static void InstallStartAmbientOnly()
        {
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene scene = EditorSceneManager.OpenScene(StartPath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                throw new System.InvalidOperationException(StartPath + " 缺少 Canvas。");
            InstallStartAmbient(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StartPath);
            if (!string.IsNullOrEmpty(returnPath) && returnPath != StartPath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("开始界面水景特效已保存，现有布局未重建。");
        }

        [MenuItem("Tools/Frog Camp/Apply Blue Transitions and Menu Music")]
        public static void ApplyBlueTransitionsAndMenuMusic()
        {
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            AudioClip menuMusic =
                AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath);
            if (menuMusic == null)
                throw new System.InvalidOperationException(
                    "找不到菜单音乐：" + MenuMusicPath);

            foreach (string path in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Canvas canvas = Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                    throw new System.InvalidOperationException(path + " 缺少 Canvas。");
                SetTransitionColor(canvas, TransitionBlue);
                if (path == InitialPath || path == StartPath || path == LobbyPath)
                    InstallMenuMusic(menuMusic);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, path);
            }

            if (!string.IsNullOrEmpty(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("蓝色场景过渡与连续菜单音乐已保存。");
        }

        private static void InstallMenuMusic(AudioClip clip)
        {
            MenuMusicPlayer player = Object.FindObjectOfType<MenuMusicPlayer>();
            GameObject target;
            if (player == null)
            {
                target = new GameObject("MenuMusicPlayer",
                    typeof(AudioSource), typeof(MenuMusicPlayer));
                player = target.GetComponent<MenuMusicPlayer>();
            }
            else
            {
                target = player.gameObject;
            }

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null) source = target.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.3f;
            player.Configure(clip, source);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(player);
        }

        private static void SetTransitionColor(Canvas canvas, Color color)
        {
            RectTransform rect = FindRect(canvas.transform, "SceneTransitionOverlay");
            if (rect == null)
                throw new System.InvalidOperationException(
                    canvas.gameObject.scene.name + " 缺少 SceneTransitionOverlay。");
            Image image = rect.GetComponent<Image>();
            if (image == null)
                throw new System.InvalidOperationException(
                    canvas.gameObject.scene.name + " 的过渡层缺少 Image。");
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        private static void InstallStartAmbient(Canvas canvas)
        {
            RectTransform page = FindRect(canvas.transform, "Page");
            if (page == null)
                throw new System.InvalidOperationException("开始界面缺少 Page。");

            RectTransform rect = FindRect(page, "StartPixelAmbientEffects");
            PixelAmbientEffects effects;
            if (rect == null)
            {
                GameObject instance = new GameObject("StartPixelAmbientEffects",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(PixelAmbientEffects));
                instance.transform.SetParent(page, false);
                rect = instance.GetComponent<RectTransform>();
                Stretch(rect);
                effects = instance.GetComponent<PixelAmbientEffects>();
            }
            else
            {
                effects = rect.GetComponent<PixelAmbientEffects>();
                if (effects == null)
                    effects = rect.gameObject.AddComponent<PixelAmbientEffects>();
            }

            RectTransform title = FindRect(page, "Title");
            effects.Configure(title);
            effects.transform.SetSiblingIndex(0);
            SerializedObject serialized = new SerializedObject(effects);
            serialized.FindProperty("pixelSize").floatValue = 10f;
            serialized.FindProperty("waterSpeed").floatValue = 0.82f;
            serialized.FindProperty("waterOpacity").floatValue = 0.92f;
            serialized.FindProperty("particleCount").intValue = 44;
            serialized.FindProperty("particleSpeed").floatValue = 0.7f;
            SetPoints(serialized.FindProperty("waterPoints"), new[]
            {
                new Vector2(0.04f, 0.18f),
                new Vector2(0.96f, 0.20f),
                new Vector2(0.035f, 0.48f),
                new Vector2(0.965f, 0.52f),
                new Vector2(0.12f, 0.075f),
                new Vector2(0.34f, 0.055f),
                new Vector2(0.66f, 0.055f),
                new Vector2(0.88f, 0.075f)
            });
            SetPoints(serialized.FindProperty("titleStarPoints"), new[]
            {
                new Vector2(0.02f, 0.72f),
                new Vector2(0.98f, 0.70f),
                new Vector2(0.12f, 0.18f),
                new Vector2(0.88f, 0.20f)
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effects);
        }

        private static void InstallLobbyAmbient(Canvas canvas)
        {
            RectTransform page = FindRect(canvas.transform, "Page");
            if (page == null)
                throw new System.InvalidOperationException("联机界面缺少 Page。");

            RectTransform rect = FindRect(page, "LobbyPixelAmbientEffects");
            PixelAmbientEffects effects;
            if (rect == null)
            {
                GameObject instance = new GameObject("LobbyPixelAmbientEffects",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(PixelAmbientEffects));
                instance.transform.SetParent(page, false);
                rect = instance.GetComponent<RectTransform>();
                Stretch(rect);
                effects = instance.GetComponent<PixelAmbientEffects>();
            }
            else
            {
                effects = rect.GetComponent<PixelAmbientEffects>();
                if (effects == null)
                    effects = rect.gameObject.AddComponent<PixelAmbientEffects>();
            }

            RectTransform title = FindRect(page, "HeaderTitle");
            effects.Configure(title);
            effects.transform.SetSiblingIndex(0);
            SerializedObject serialized = new SerializedObject(effects);
            serialized.FindProperty("pixelSize").floatValue = 10f;
            serialized.FindProperty("waterSpeed").floatValue = 0.82f;
            serialized.FindProperty("waterOpacity").floatValue = 0.92f;
            serialized.FindProperty("particleCount").intValue = 44;
            serialized.FindProperty("particleSpeed").floatValue = 0.7f;
            SetPoints(serialized.FindProperty("waterPoints"), new[]
            {
                new Vector2(0.04f, 0.18f),
                new Vector2(0.96f, 0.20f),
                new Vector2(0.035f, 0.48f),
                new Vector2(0.965f, 0.52f),
                new Vector2(0.12f, 0.075f),
                new Vector2(0.34f, 0.055f),
                new Vector2(0.66f, 0.055f),
                new Vector2(0.88f, 0.075f)
            });
            SetPoints(serialized.FindProperty("titleStarPoints"), new[]
            {
                new Vector2(0.02f, 0.72f),
                new Vector2(0.98f, 0.70f),
                new Vector2(0.12f, 0.18f),
                new Vector2(0.88f, 0.20f)
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effects);
        }

        private static void InstallTransition(Canvas canvas)
        {
            RectTransform rect = FindRect(canvas.transform, "SceneTransitionOverlay");
            SceneTransitionOverlay transition;
            CanvasGroup group;
            if (rect == null)
            {
                GameObject instance = new GameObject("SceneTransitionOverlay",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                    typeof(CanvasGroup), typeof(SceneTransitionOverlay));
                instance.transform.SetParent(canvas.transform, false);
                rect = instance.GetComponent<RectTransform>();
                Stretch(rect);
                Image image = instance.GetComponent<Image>();
                image.color = TransitionBlue;
                image.raycastTarget = true;
                group = instance.GetComponent<CanvasGroup>();
                transition = instance.GetComponent<SceneTransitionOverlay>();
            }
            else
            {
                group = rect.GetComponent<CanvasGroup>();
                if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
                transition = rect.GetComponent<SceneTransitionOverlay>();
                if (transition == null)
                    transition = rect.gameObject.AddComponent<SceneTransitionOverlay>();
            }
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            transition.Configure(group, rect);
            rect.SetAsLastSibling();
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(transition);
        }

        private static void SetPoints(SerializedProperty property, Vector2[] points)
        {
            property.arraySize = points.Length;
            for (int index = 0; index < points.Length; index++)
                property.GetArrayElementAtIndex(index).vector2Value = points[index];
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
