using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Editor
{
    public static class PondSceneEffectsInstaller
    {
        private const string GameScenePath =
            "Assets/Scenes/游戏界面.unity";

        [MenuItem("Tools/Frog Camp/Add Pond Environment Motion")]
        public static void InstallWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog("添加荷塘环境动效",
                    "这会给现有地图元素添加视觉动画组件，并新增可编辑的" +
                    "水花、星光、气泡和风线层；不会重建地图或添加碰撞体。",
                    "添加动效", "取消"))
                return;
            Install();
        }

        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene scene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);

            RectTransform artwork = GameObject.Find("CampMap")?
                .transform.Find("MapArtwork") as RectTransform;
            if (artwork == null)
                throw new System.InvalidOperationException(
                    "游戏界面缺少 CampMap/MapArtwork。");
            RectTransform elements = artwork.Find(
                "MapElements") as RectTransform;
            if (elements == null)
                throw new System.InvalidOperationException(
                    "游戏界面缺少 MapArtwork/MapElements。");

            InstallAmbientLayer(artwork);
            InstallElementMotions(elements);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(returnPath) &&
                returnPath != GameScenePath)
            {
                EditorSceneManager.OpenScene(
                    returnPath, OpenSceneMode.Single);
            }
            Debug.Log("POND_ENVIRONMENT_EFFECTS_SAVED");
        }

        private static void InstallAmbientLayer(RectTransform artwork)
        {
            Transform existing = artwork.Find("PondAmbientEffects");
            GameObject target;
            if (existing == null)
            {
                target = new GameObject("PondAmbientEffects",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(PixelAmbientEffects));
                target.transform.SetParent(artwork, false);
            }
            else
            {
                target = existing.gameObject;
                if (target.GetComponent<PixelAmbientEffects>() == null)
                    target.AddComponent<PixelAmbientEffects>();
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            Stretch(rect);
            rect.SetSiblingIndex(Mathf.Min(1,
                artwork.childCount - 1));
            PixelAmbientEffects effects =
                target.GetComponent<PixelAmbientEffects>();
            effects.Configure(null);

            SerializedObject serialized = new SerializedObject(effects);
            serialized.FindProperty("pixelSize").floatValue = 9.5f;
            serialized.FindProperty("waterSpeed").floatValue = 0.72f;
            serialized.FindProperty("waterOpacity").floatValue = 0.92f;
            serialized.FindProperty("particleCount").intValue = 100;
            serialized.FindProperty("particleSpeed").floatValue = 0.72f;
            serialized.FindProperty("starSpeed").floatValue = 0.92f;
            serialized.FindProperty("starOpacity").floatValue = 0.96f;
            serialized.FindProperty("starsUseFullRect").boolValue = true;
            serialized.FindProperty("windLineCount").intValue = 26;
            serialized.FindProperty("windSpeed").floatValue = 0.72f;
            serialized.FindProperty("windOpacity").floatValue = 0.62f;
            serialized.FindProperty("waterColor").colorValue =
                new Color(0.80f, 0.97f, 1f, 0.82f);
            serialized.FindProperty("particleColor").colorValue =
                new Color(0.96f, 1f, 1f, 0.88f);
            serialized.FindProperty("starColor").colorValue =
                new Color(1f, 0.95f, 0.62f, 0.92f);
            serialized.FindProperty("windColor").colorValue =
                new Color(0.90f, 1f, 0.94f, 0.48f);

            SetPoints(serialized.FindProperty("waterPoints"), new[]
            {
                new Vector2(0.08f, 0.15f),
                new Vector2(0.22f, 0.28f),
                new Vector2(0.37f, 0.16f),
                new Vector2(0.52f, 0.30f),
                new Vector2(0.68f, 0.16f),
                new Vector2(0.84f, 0.30f),
                new Vector2(0.94f, 0.17f),
                new Vector2(0.13f, 0.61f),
                new Vector2(0.31f, 0.76f),
                new Vector2(0.49f, 0.66f),
                new Vector2(0.72f, 0.78f),
                new Vector2(0.90f, 0.62f)
            });
            SetPoints(serialized.FindProperty("titleStarPoints"), new[]
            {
                new Vector2(0.07f, 0.83f),
                new Vector2(0.18f, 0.52f),
                new Vector2(0.29f, 0.91f),
                new Vector2(0.41f, 0.57f),
                new Vector2(0.53f, 0.86f),
                new Vector2(0.64f, 0.46f),
                new Vector2(0.75f, 0.91f),
                new Vector2(0.87f, 0.56f),
                new Vector2(0.95f, 0.78f),
                new Vector2(0.12f, 0.34f),
                new Vector2(0.35f, 0.38f),
                new Vector2(0.58f, 0.25f),
                new Vector2(0.81f, 0.35f)
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effects);
        }

        private static void InstallElementMotions(RectTransform elements)
        {
            AddSway(elements, "UpperLeftGrassA", 0.86f, 0.2f, 4.2f);
            AddSway(elements, "UpperLeftGrassB", 0.78f, 1.4f, 5.0f);
            AddSway(elements, "UpperLeftGrassC", 0.94f, 2.8f, 4.5f);
            AddSway(elements, "UpperLeftGrassD", 0.73f, 4.1f, 4.8f);
            AddSway(elements, "UpperLeftReedA", 0.70f, 0.8f, 6.0f);
            AddSway(elements, "UpperLeftReedB", 0.82f, 2.2f, 7.2f);
            AddSway(elements, "UpperLeftReedC", 0.66f, 3.5f, 5.8f);
            AddSway(elements, "UpperLeftReedD", 0.88f, 5.0f, 6.6f);
            AddSway(elements, "UpperRightTwig", 0.64f, 3.2f, 4.5f);

            RemoveMotion(elements, "CenterLily");
            AddMotion(elements, "LeftLily",
                PondElementMotion.MotionKind.Float, 0.76f, 2.1f,
                new Vector2(4.0f, 3.2f), 2.8f, 0.03f, false);
            AddMotion(elements, "RightLily",
                PondElementMotion.MotionKind.Float, 0.71f, 4.0f,
                new Vector2(4.6f, 3.6f), 2.4f, 0.028f, false);
            AddMotion(elements, "BottomLeftLeafCluster",
                PondElementMotion.MotionKind.Float, 0.62f, 1.2f,
                new Vector2(3.0f, 2.5f), 2.0f, 0.022f, false);
            AddMotion(elements, "BottomRightLeafCluster",
                PondElementMotion.MotionKind.Float, 0.65f, 3.7f,
                new Vector2(3.4f, 2.7f), 2.1f, 0.022f, false);

            AddMotion(elements, "BottomLeftFlowerA",
                PondElementMotion.MotionKind.Bloom, 1.02f, 0.4f,
                new Vector2(0.4f, 2.0f), 2.8f, 0.10f, false);
            AddMotion(elements, "BottomLeftFlowerB",
                PondElementMotion.MotionKind.Bloom, 0.91f, 2.9f,
                new Vector2(0.4f, 1.8f), 3.2f, 0.11f, false);

            AddMotion(elements, "BottomRightFeatherA",
                PondElementMotion.MotionKind.Feather, 0.72f, 0.7f,
                new Vector2(6.5f, 3.4f), 11f, 0.025f, false);
            AddMotion(elements, "BottomRightFeatherB",
                PondElementMotion.MotionKind.Feather, 0.63f, 3.4f,
                new Vector2(8.0f, 4.2f), 14f, 0.03f, false);

            AddMotion(elements, "BottomLeftInsectA",
                PondElementMotion.MotionKind.Insect, 1.18f, 0.2f,
                new Vector2(18f, 10f), 2.2f, 0.10f, true);
            AddMotion(elements, "BottomLeftInsectB",
                PondElementMotion.MotionKind.Insect, 0.94f, 2.3f,
                new Vector2(25f, 14f), 2.8f, 0.12f, true);
            AddMotion(elements, "BottomLeftInsectC",
                PondElementMotion.MotionKind.Insect, 1.07f, 4.6f,
                new Vector2(16f, 19f), 2.5f, 0.10f, true);

            AddFish(elements, "FishA", 0.38f, 0.4f,
                new Vector2(38f, 3.0f));
            AddFish(elements, "FishB", 0.46f, 2.0f,
                new Vector2(48f, 4.0f));
            AddFish(elements, "FishC", 0.33f, 4.2f,
                new Vector2(42f, 3.4f));
            AddFish(elements, "FishD", 0.41f, 5.5f,
                new Vector2(34f, 4.2f));
        }

        private static void AddSway(RectTransform root, string name,
            float speed, float phase, float rotation)
        {
            RectTransform target = root.Find(name) as RectTransform;
            if (target != null)
                SetPivotPreservePosition(target, new Vector2(0.5f, 0.08f));
            AddMotion(root, name, PondElementMotion.MotionKind.Sway,
                speed, phase, new Vector2(1.8f, 0.9f),
                rotation, 0.012f, false);
        }

        private static void AddFish(RectTransform root, string name,
            float speed, float phase, Vector2 position)
        {
            AddMotion(root, name, PondElementMotion.MotionKind.Fish,
                speed, phase, position, 1.5f, 0.008f, true);
        }

        private static void AddMotion(RectTransform root, string name,
            PondElementMotion.MotionKind kind, float speed, float phase,
            Vector2 position, float rotation, float scale, bool flip)
        {
            Transform child = root.Find(name);
            if (child == null) return;
            PondElementMotion motion =
                child.GetComponent<PondElementMotion>();
            if (motion == null)
                motion = child.gameObject.AddComponent<PondElementMotion>();
            motion.Configure(kind, speed, phase, position,
                rotation, scale, flip);
            EditorUtility.SetDirty(motion);
        }

        private static void RemoveMotion(
            RectTransform root, string name)
        {
            Transform child = root.Find(name);
            if (child == null) return;
            PondElementMotion motion =
                child.GetComponent<PondElementMotion>();
            if (motion != null)
                Object.DestroyImmediate(motion);
        }

        private static void SetPivotPreservePosition(
            RectTransform rect, Vector2 pivot)
        {
            Vector2 delta = pivot - rect.pivot;
            Vector2 size = rect.rect.size;
            rect.anchoredPosition += new Vector2(
                delta.x * size.x * rect.localScale.x,
                delta.y * size.y * rect.localScale.y);
            rect.pivot = pivot;
            EditorUtility.SetDirty(rect);
        }

        private static void SetPoints(
            SerializedProperty property, Vector2[] points)
        {
            property.arraySize = points.Length;
            for (int index = 0; index < points.Length; index++)
            {
                property.GetArrayElementAtIndex(index).vector2Value =
                    points[index];
            }
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
