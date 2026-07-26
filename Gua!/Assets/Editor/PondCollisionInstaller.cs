using System.IO;
using FrogCamp.Game;
using FrogCamp.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.Editor
{
    public static class PondCollisionInstaller
    {
        private const string GameScenePath = "Assets/Scenes/游戏界面.unity";
        private const string ConfigFolder = "Assets/Resources";
        private const string ConfigPath =
            ConfigFolder + "/PondCollisionConfig.asset";

        [MenuItem("Tools/Frog Camp/Install Pond Collision Volumes")]
        public static void InstallWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog(
                    "安装荷塘碰撞体积",
                    "将在 MapArtwork 下保存一个可选中查看的 CollisionVolumes，" +
                    "不会重建或移动现有地图 UI。是否继续？",
                    "安装", "取消"))
                return;
            Install();
        }

        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Scene gameScene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Single);
            Transform artwork =
                GameObject.Find("CampMap")?.transform.Find("MapArtwork");
            if (artwork == null)
                throw new System.InvalidOperationException(
                    "游戏界面缺少 CampMap/MapArtwork。");

            Transform existing = artwork.Find("CollisionVolumes");
            GameObject collisionObject;
            if (existing == null)
            {
                collisionObject = new GameObject(
                    "CollisionVolumes", typeof(RectTransform));
                collisionObject.transform.SetParent(artwork, false);
            }
            else
            {
                collisionObject = existing.gameObject;
            }

            RectTransform rect =
                collisionObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            PondCollisionDebugView debugView =
                collisionObject.GetComponent<PondCollisionDebugView>();
            if (debugView == null)
                debugView =
                    collisionObject.AddComponent<PondCollisionDebugView>();
            debugView.SetCollisionConfig(LoadOrCreateConfig());
            collisionObject.transform.SetAsLastSibling();
            RemoveLegacyMarkers(rect);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(returnPath) &&
                File.Exists(returnPath) &&
                returnPath != GameScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);

            Debug.Log(
                "POND_COLLISION_INSTALLED: 边缘石头及指定地图元素已接入权威碰撞。");
        }

        private static PondCollisionConfig LoadOrCreateConfig()
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
            PondCollisionConfig config =
                AssetDatabase.LoadAssetAtPath<PondCollisionConfig>(ConfigPath);
            if (config != null)
            {
                if (config.RockBoundary.Count < 3)
                {
                    config.ReplaceBoundary(PondObstacleMap.DefaultBoundary);
                    EditorUtility.SetDirty(config);
                }
                if (config.ObstacleRegions.Count == 0)
                {
                    config.ReplaceObstacleRegions(
                        PondObstacleMap.Definitions);
                    EditorUtility.SetDirty(config);
                }
                return config;
            }

            config = ScriptableObject.CreateInstance<PondCollisionConfig>();
            config.ReplaceBoundary(PondObstacleMap.DefaultBoundary);
            config.ReplaceObstacleRegions(PondObstacleMap.Definitions);
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void RemoveLegacyMarkers(RectTransform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }
    }
}
