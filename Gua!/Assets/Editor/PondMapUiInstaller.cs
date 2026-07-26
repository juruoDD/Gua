using System.IO;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Editor
{
    public static class PondMapUiInstaller
    {
        private const string GameScenePath = "Assets/Scenes/游戏界面.unity";
        private const string MapAssetFolder = "Assets/UI/地图/";

        [MenuItem("Tools/Frog Camp/Rebuild Pond Map Artwork")]
        public static void InstallWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog("重建荷塘地图视觉层",
                    "这会替换 CampMap/MapArtwork 下的背景和装饰排布，" +
                    "不会修改角色、HUD、联机逻辑或添加碰撞体。是否继续？",
                    "重建地图", "取消"))
                return;
            Install();
        }

        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ConfigureMapTextureImports();
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene gameScene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);
            RectTransform campMap =
                GameObject.Find("CampMap")?.GetComponent<RectTransform>();
            if (campMap == null)
                throw new System.InvalidOperationException(
                    "游戏界面缺少 CampMap。");

            BuildMapArtwork(campMap);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(returnPath) && File.Exists(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("像素荷塘地图已保存到游戏 Scene；未添加碰撞体。");
        }

        private static void BuildMapArtwork(RectTransform campMap)
        {
            Transform oldArtwork = campMap.Find("MapArtwork");
            if (oldArtwork != null)
                Object.DestroyImmediate(oldArtwork.gameObject);

            GameObject artworkObject = new GameObject("MapArtwork",
                typeof(RectTransform));
            artworkObject.transform.SetParent(campMap, false);
            RectTransform artwork = artworkObject.GetComponent<RectTransform>();
            Stretch(artwork);
            artwork.SetSiblingIndex(0);

            Image background = CreateImage(artwork, "PondBackground",
                "背景", new Vector2(0.5f, 0.5f), new Vector2(960f, 540f));
            Stretch(background.rectTransform);
            background.preserveAspect = false;
            background.transform.SetSiblingIndex(0);

            RectTransform elements = CreateLayer(artwork, "MapElements");

            // Reference image coordinates are measured in its native
            // 1672x941 space. Most source elements are authored at 3x size.
            CreateReferenceImage(elements, "UpperLeftGrassA", "草1",
                new Vector2(150f, 188f));
            CreateReferenceImage(elements, "UpperLeftGrassB", "草2",
                new Vector2(212f, 89f));
            CreateReferenceImage(elements, "UpperLeftReedA", "芦苇",
                new Vector2(236f, 131f));
            CreateReferenceImage(elements, "UpperLeftReedB", "芦苇",
                new Vector2(307f, 35f));
            CreateReferenceImage(elements, "UpperLeftGrassC", "草1",
                new Vector2(319f, 188f));
            CreateReferenceImage(elements, "UpperLeftReedC", "芦苇",
                new Vector2(412f, 62f));
            CreateReferenceImage(elements, "UpperLeftReedD", "芦苇",
                new Vector2(412f, 152f));
            CreateReferenceImage(elements, "UpperLeftGrassD", "草1",
                new Vector2(527f, 98f));

            CreateReferenceImage(elements, "CenterLily", "大荷叶",
                new Vector2(515f, 227f));
            CreateReferenceImage(elements, "LeftLily", "荷叶2",
                new Vector2(77f, 362f));
            CreateReferenceImage(elements, "RightLily", "荷叶1",
                new Vector2(1240f, 406f));

            CreateReferenceImage(elements, "UpperRightLeafBase", "大荷叶",
                new Vector2(1147f, 37f), 0.194f);
            CreateReferenceImage(elements, "UpperRightLeafRoof", "荷叶1",
                new Vector2(1208f, 43f), 0.5f);
            CreateReferenceImage(elements, "UpperRightPlank", "木板1",
                new Vector2(1179f, 126f));
            CreateReferenceImage(elements, "UpperRightCrateA", "木板2",
                new Vector2(1265f, 132f), 0.22f);
            CreateReferenceImage(elements, "UpperRightCrateB", "木板2",
                new Vector2(1345f, 158f), 0.22f);
            CreateReferenceImage(elements, "UpperRightCrateC", "木板2",
                new Vector2(1322f, 210f), 0.20f);
            CreateReferenceImage(elements, "UpperRightCrateD", "木板2",
                new Vector2(1270f, 238f), 0.19f, 90f);
            CreateReferenceImage(elements, "UpperRightTwig", "树枝",
                new Vector2(1257f, 68f), 0.13f, 10f);

            CreateReferenceImage(elements, "BottomLeftLeafCluster", "荷叶堆2",
                new Vector2(121f, 620f));
            CreateReferenceImage(elements, "BottomLeftFlowerA", "花1",
                new Vector2(183f, 689f));
            CreateReferenceImage(elements, "BottomLeftFlowerB", "花2",
                new Vector2(244f, 746f));
            CreateReferenceImage(elements, "BottomLeftInsectA", "飞虫2",
                new Vector2(150f, 619f));
            CreateReferenceImage(elements, "BottomLeftInsectB", "飞虫1",
                new Vector2(284f, 637f));
            CreateReferenceImage(elements, "BottomLeftInsectC", "飞虫3",
                new Vector2(380f, 706f));

            CreateReferenceImage(elements, "BottomRightLeafCluster", "荷叶堆1",
                new Vector2(1119f, 599f));
            CreateReferenceImage(elements, "BottomRightNest", "鸟巢",
                new Vector2(1186f, 614f));
            CreateReferenceImage(elements, "BottomRightFeatherA", "羽毛1",
                new Vector2(1271f, 663f));
            CreateReferenceImage(elements, "BottomRightFeatherB", "羽毛2",
                new Vector2(1378f, 727f));

            CreateReferenceImage(elements, "FishA", "鱼2",
                new Vector2(240f, 323f));
            CreateReferenceImage(elements, "FishB", "鱼1",
                new Vector2(486f, 633f));
            CreateReferenceImage(elements, "FishC", "鱼2",
                new Vector2(990f, 735f));
            CreateReferenceImage(elements, "FishD", "鱼1",
                new Vector2(1403f, 474f));

            SetLegacyMapActive("HorizontalRoad", false);
            SetLegacyMapActive("VerticalRoad", false);
            SetLegacyMapActive("FlagPole", false);
        }

        private static RectTransform CreateLayer(Transform parent, string name)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            instance.transform.SetParent(parent, false);
            RectTransform rect = instance.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private static Image CreateImage(Transform parent, string objectName,
            string spriteName, Vector2 anchor, Vector2 size,
            float rotation = 0f)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                MapAssetFolder + spriteName + ".png");
            if (sprite == null)
                throw new System.InvalidOperationException(
                    "找不到地图图片：" + spriteName);

            GameObject instance = new GameObject(objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            instance.transform.SetParent(parent, false);
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            Image image = instance.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateReferenceImage(Transform parent,
            string objectName, string spriteName, Vector2 referenceTopLeft,
            float sourceScale = 1f / 3f, float rotation = 0f)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                MapAssetFolder + spriteName + ".png");
            if (sprite == null)
                throw new System.InvalidOperationException(
                    "找不到地图图片：" + spriteName);

            const float referenceWidth = 1672f;
            const float referenceHeight = 941f;
            const float logicalWidth = 960f;
            const float logicalHeight = 540f;
            Vector2 referenceSize = new Vector2(
                sprite.texture.width * sourceScale,
                sprite.texture.height * sourceScale);
            Vector2 referenceCenter = referenceTopLeft + referenceSize * 0.5f;
            Vector2 anchor = new Vector2(
                referenceCenter.x / referenceWidth,
                1f - referenceCenter.y / referenceHeight);
            Vector2 logicalSize = new Vector2(
                referenceSize.x * logicalWidth / referenceWidth,
                referenceSize.y * logicalHeight / referenceHeight);
            return CreateImage(parent, objectName, spriteName,
                anchor, logicalSize, rotation);
        }

        private static void ConfigureMapTextureImports()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D", new[] { MapAssetFolder.TrimEnd('/') });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("参考图.png")) continue;
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = importer.filterMode != FilterMode.Point ||
                    importer.mipmapEnabled ||
                    importer.textureCompression !=
                    TextureImporterCompression.Uncompressed;
                if (!changed) continue;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void SetLegacyMapActive(string objectName, bool active)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null) target.SetActive(active);
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
