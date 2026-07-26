using System.IO;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Editor
{
    public static class TaikoTrackEffectsInstaller
    {
        private const string GameScenePath =
            "Assets/Scenes/游戏界面.unity";
        private const string UiFolder = "Assets/UI/";
        private const string FontPath =
            "Assets/Fonts/DingTalk JinBuTi.ttf";

        [MenuItem("Tools/Frog Camp/Install Taiko Track Effects")]
        public static void InstallWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog(
                    "安装太鼓式节奏反馈",
                    "只会给现有 RhythmCommandTrack 接入四张命令图标，" +
                    "并在 BeatTarget 下增加命中特效和“好！”反馈；" +
                    "不会重建或移动你手调的轨道、判定圈和音符槽。",
                    "安装", "取消"))
                return;
            Install();
        }

        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ConfigureIconImports();

            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene scene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);
            GameObject trackObject =
                GameObject.Find("RhythmCommandTrack");
            RhythmCommandTrack track =
                trackObject?.GetComponent<RhythmCommandTrack>();
            Transform target = trackObject?.transform.Find("BeatTarget");
            if (track == null || target == null)
                throw new System.InvalidOperationException(
                    "游戏界面缺少 RhythmCommandTrack/BeatTarget。");

            TaikoHitEffectGraphic effect = CreateHitEffect(target);
            Text judgement = CreateJudgement(target);
            WireReferences(track, effect, judgement);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(returnPath) &&
                returnPath != GameScenePath &&
                File.Exists(returnPath))
                EditorSceneManager.OpenScene(
                    returnPath, OpenSceneMode.Single);
            Debug.Log(
                "TAIKO_TRACK_EFFECTS_INSTALLED: 已保留现有轨道布局并接入太鼓式反馈。");
        }

        private static TaikoHitEffectGraphic CreateHitEffect(
            Transform target)
        {
            Transform existing = target.Find("HitBurst");
            GameObject instance;
            if (existing == null)
            {
                instance = new GameObject("HitBurst",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TaikoHitEffectGraphic));
                instance.transform.SetParent(target, false);
            }
            else
            {
                instance = existing.gameObject;
                if (instance.GetComponent<TaikoHitEffectGraphic>() == null)
                    instance.AddComponent<TaikoHitEffectGraphic>();
            }

            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(360f, 360f);
            rect.localScale = Vector3.one;
            instance.transform.SetSiblingIndex(0);
            TaikoHitEffectGraphic effect =
                instance.GetComponent<TaikoHitEffectGraphic>();
            effect.raycastTarget = false;
            effect.ApplyStrongPixelStyle();
            return effect;
        }

        private static Text CreateJudgement(Transform target)
        {
            Transform existing = target.Find("JudgementFeedback");
            GameObject instance;
            if (existing == null)
            {
                instance = new GameObject("JudgementFeedback",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Text), typeof(Shadow));
                instance.transform.SetParent(target, false);
            }
            else
            {
                instance = existing.gameObject;
                if (instance.GetComponent<Text>() == null)
                    instance.AddComponent<Text>();
                if (instance.GetComponent<Shadow>() == null)
                    instance.AddComponent<Shadow>();
            }

            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 96f);
            rect.sizeDelta = new Vector2(190f, 68f);
            rect.localScale = Vector3.one;
            Text text = instance.GetComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            text.fontSize = 42;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.84f, 0.18f, 1f);
            text.text = "好！";
            text.raycastTarget = false;
            Shadow shadow = instance.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.22f, 0.12f, 0.06f, 0.9f);
            shadow.effectDistance = new Vector2(3f, -3f);
            instance.SetActive(false);
            return text;
        }

        private static void WireReferences(RhythmCommandTrack track,
            TaikoHitEffectGraphic effect, Text judgement)
        {
            SerializedObject serialized = new SerializedObject(track);
            SerializedProperty sprites =
                serialized.FindProperty("commandSprites");
            sprites.arraySize = 10;
            string[] names = { "左手", "右手", "左脚", "右脚" };
            for (int index = 0; index < names.Length; index++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    UiFolder + names[index] + ".png");
                if (sprite != null)
                    sprites.GetArrayElementAtIndex(index)
                        .objectReferenceValue = sprite;
            }
            serialized.FindProperty("hitEffect").objectReferenceValue =
                effect;
            serialized.FindProperty("judgementRoot")
                .objectReferenceValue = judgement.rectTransform;
            serialized.FindProperty("judgementText")
                .objectReferenceValue = judgement;
            serialized.FindProperty("leadTime").floatValue = 3f;
            serialized.FindProperty("passWindow").floatValue = 0.24f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(track);
        }

        private static void ConfigureIconImports()
        {
            string[] names = { "左手", "右手", "左脚", "右脚" };
            foreach (string name in names)
            {
                string path = UiFolder + name + ".png";
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType =
                    TextureImporterType.Sprite;
                importer.spriteImportMode =
                    SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
