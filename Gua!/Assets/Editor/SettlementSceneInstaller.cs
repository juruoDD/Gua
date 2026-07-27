using System.Linq;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Editor
{
    public static class SettlementSceneInstaller
    {
        private const string ScenePath = "Assets/Scenes/结算页面.unity";

        [MenuItem("Tools/Frog Camp/Install Settlement Scene")]
        public static void Install()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RectTransform page = FindRect("Page");
            if (page == null)
                throw new System.InvalidOperationException("结算页面缺少 Page。");

            SettlementSceneController controller =
                Object.FindObjectOfType<SettlementSceneController>();
            if (controller == null)
                controller = page.gameObject.AddComponent<SettlementSceneController>();

            Button backButton = FindButton("BackButton");
            if (backButton == null)
                backButton = CreateBackButton(page);

            RectTransform[] lilyPads = Enumerable.Range(1, 4)
                .Select(index => FindRect(index.ToString())).ToArray();
            if (lilyPads.Any(item => item == null))
                throw new System.InvalidOperationException(
                    "结算页面需要名为 1、2、3、4 的四片荷叶。");

            RawImage[] frogImages = new RawImage[4];
            Text[] playerNameTexts = new Text[4];
            Text[] resultTexts = new Text[4];
            SettlementResultEffect[] resultEffects =
                new SettlementResultEffect[4];
            Font font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Fonts/016-SSRuiYuanTi.ttf");
            for (int index = 0; index < lilyPads.Length; index++)
            {
                BuildEditableSlot(lilyPads[index], index, font,
                    out frogImages[index], out playerNameTexts[index],
                    out resultTexts[index]);
                resultEffects[index] = EnsureResultEffect(lilyPads[index]);
            }
            for (int index = 1; index < lilyPads.Length; index++)
                CopySlotFormatting(frogImages[0], playerNameTexts[0],
                    resultTexts[0], frogImages[index], playerNameTexts[index],
                    resultTexts[index]);
            ConfigureAmbientEffects(page);

            SerializedObject serialized = new SerializedObject(controller);
            SetArray(serialized.FindProperty("lilyPads"), lilyPads);
            SetArray(serialized.FindProperty("frogImages"), frogImages);
            SetArray(serialized.FindProperty("playerNameTexts"), playerNameTexts);
            SetArray(serialized.FindProperty("resultTexts"), resultTexts);
            SetArray(serialized.FindProperty("resultEffects"), resultEffects);
            serialized.FindProperty("backButton").objectReferenceValue = backButton;
            serialized.FindProperty("greenSalute").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Frog/敬礼.png");
            serialized.FindProperty("greenDeath").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Frog/绿色死亡.png");
            serialized.FindProperty("pinkFallback").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Frog/粉色待机.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureSceneInBuildSettings();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("结算页面已接入胜负结果、玩家名、青蛙动画和返回按钮。");
        }

        private static void CopySlotFormatting(RawImage sourceFrog,
            Text sourceName, Text sourceResult, RawImage targetFrog,
            Text targetName, Text targetResult)
        {
            CopyRect(sourceFrog.rectTransform, targetFrog.rectTransform);
            Texture targetTexture = targetFrog.texture;
            Rect targetUv = targetFrog.uvRect;
            EditorUtility.CopySerialized(sourceFrog, targetFrog);
            targetFrog.texture = targetTexture;
            targetFrog.uvRect = targetUv;

            CopyTextFormat(sourceName, targetName);
            CopyTextFormat(sourceResult, targetResult);
        }

        private static void CopyTextFormat(Text source, Text target)
        {
            string text = target.text;
            CopyRect(source.rectTransform, target.rectTransform);
            EditorUtility.CopySerialized(source, target);
            target.text = text;
        }

        private static void CopyRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static SettlementResultEffect EnsureResultEffect(
            RectTransform lily)
        {
            Transform existing = lily.Find("ResultEffect");
            GameObject instance;
            if (existing == null)
            {
                instance = new GameObject("ResultEffect",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(SettlementResultEffect));
                instance.transform.SetParent(lily, false);
            }
            else instance = existing.gameObject;
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0f, 58f);
            rect.sizeDelta = new Vector2(360f, 420f);
            rect.SetSiblingIndex(0);
            SettlementResultEffect effect =
                instance.GetComponent<SettlementResultEffect>();
            effect.raycastTarget = false;
            return effect;
        }

        private static void ConfigureAmbientEffects(RectTransform page)
        {
            PixelAmbientEffects effects =
                Object.FindObjectOfType<PixelAmbientEffects>();
            Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI/13 2.png");
            Image title = Object.FindObjectsOfType<Image>(true)
                .FirstOrDefault(image => image.sprite == titleSprite);
            if (effects == null || title == null) return;
            effects.gameObject.name = "SettlementPixelAmbientEffects";
            effects.Configure(title.rectTransform);
            effects.rectTransform.SetSiblingIndex(2);
            EditorUtility.SetDirty(effects);
        }

        private static void BuildEditableSlot(RectTransform lily, int index,
            Font font, out RawImage frog, out Text playerName, out Text result)
        {
            Transform frogTransform = lily.Find("ResultFrog");
            if (frogTransform == null)
            {
                GameObject instance = new GameObject("ResultFrog",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                instance.transform.SetParent(lily, false);
                frogTransform = instance.transform;
                frog = frogTransform.GetComponent<RawImage>();
                RectTransform frogRect = frog.rectTransform;
                frogRect.anchorMin = frogRect.anchorMax = new Vector2(.5f, .5f);
                frogRect.anchoredPosition = new Vector2(0f, 118f);
                frogRect.sizeDelta = new Vector2(132f, 264f);
                frog.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Frog/敬礼.png");
                frog.uvRect = new Rect(0f, 0f, 1f / 8f, 1f);
                frog.raycastTarget = false;
            }
            else frog = frogTransform.GetComponent<RawImage>();

            playerName = CreateEditableText(lily, "PlayerName",
                "玩家" + (index + 1), 31, new Vector2(.5f, -.18f),
                new Vector2(300f, 54f), new Color32(39, 65, 55, 255), font);
            result = CreateEditableText(lily, "Result", "胜利！", 34,
                new Vector2(.5f, -.38f), new Vector2(300f, 58f),
                new Color32(36, 116, 69, 255), font);
        }

        private static Text CreateEditableText(RectTransform parent, string name,
            string value, int fontSize, Vector2 anchor, Vector2 size,
            Color color, Font font)
        {
            Transform existing = parent.Find(name);
            GameObject instance;
            if (existing == null)
            {
                instance = new GameObject(name, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                instance.transform.SetParent(parent, false);
                RectTransform rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = anchor;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = size;
                Text text = instance.GetComponent<Text>();
                text.text = value;
                text.font = font;
                text.fontSize = fontSize;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = color;
                text.raycastTarget = false;
                return text;
            }
            return existing.GetComponent<Text>();
        }

        private static void SetArray<T>(SerializedProperty property, T[] values)
            where T : Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static Button CreateBackButton(RectTransform parent)
        {
            GameObject instance = new GameObject("BackButton",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            instance.transform.SetParent(parent, false);
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(165f, -72f);
            rect.sizeDelta = new Vector2(224f, 80f);
            Image image = instance.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI/返回按钮.png");
            image.preserveAspect = true;
            Button button = instance.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static RectTransform FindRect(string objectName)
        {
            return Object.FindObjectsOfType<RectTransform>(true)
                .FirstOrDefault(item => item.name == objectName);
        }

        private static Button FindButton(string objectName)
        {
            RectTransform rect = FindRect(objectName);
            return rect == null ? null : rect.GetComponent<Button>();
        }

        private static void EnsureSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(item => item.path == ScenePath)) return;
            EditorBuildSettings.scenes = scenes.Concat(new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            }).ToArray();
        }
    }
}
