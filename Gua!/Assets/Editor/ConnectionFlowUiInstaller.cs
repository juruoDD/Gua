using FrogCamp.Networking;
using FrogCamp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.Editor
{
    public static class ConnectionFlowUiInstaller
    {
        private const string StartPath = "Assets/Scenes/开始界面.unity";
        private const string LobbyPath = "Assets/Scenes/联机界面.unity";

        [MenuItem("Tools/Frog Camp/Simplify Connection Flow")]
        public static void Install()
        {
            string returnPath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            UpgradeStartScene();
            UpgradeLobbyScene();
            if (!string.IsNullOrEmpty(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            Debug.Log("开始界面已接入名称与 IP，联机界面 ConnectPanel 已删除。");
        }

        private static void UpgradeStartScene()
        {
            Scene scene = EditorSceneManager.OpenScene(StartPath, OpenSceneMode.Single);
            StartSceneController controller =
                Object.FindObjectOfType<StartSceneController>();
            if (controller == null)
                throw new System.InvalidOperationException("开始界面缺少 StartSceneController。");

            RectTransform joinCard = FindRect(controller.transform, "JoinCard");
            if (joinCard == null)
                throw new System.InvalidOperationException("开始界面缺少 JoinCard。");

            Text title = FindComponent<Text>(joinCard, "JoinTitle");
            Text nameLabel = FindComponent<Text>(joinCard, "NameLabel");
            InputField nameInput = FindComponent<InputField>(joinCard, "NameInput");
            Button createButton = FindComponent<Button>(joinCard, "CreateButton");
            Button joinButton = FindComponent<Button>(joinCard, "JoinButton") ??
                FindComponent<Button>(joinCard, "BrowseButton");
            Text status = FindComponent<Text>(joinCard, "Status");
            if (nameInput == null || createButton == null ||
                joinButton == null || status == null)
                throw new System.InvalidOperationException("JoinCard 现有控件不完整。");

            joinButton.gameObject.name = "JoinButton";
            CampUiFactory.ButtonLabel(joinButton).text = "加入房间";
            CampUiFactory.ButtonLabel(createButton).text = "创建房间";
            if (title != null) title.text = "进入通讯频道";
            if (nameLabel != null) nameLabel.text = "玩家名称";

            Text addressLabel = FindComponent<Text>(joinCard, "AddressLabel");
            if (addressLabel == null)
            {
                addressLabel = CampUiFactory.Text(joinCard, "AddressLabel", "房主 IP",
                    18, CampUiFactory.Leaf, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            }
            InputField addressInput = FindComponent<InputField>(joinCard, "AddressInput");
            if (addressInput == null)
            {
                addressInput = CampUiFactory.Input(joinCard, "AddressInput",
                    "例如 192.168.1.20", Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, 45);
            }
            Text localIp = FindComponent<Text>(joinCard, "LocalIp");
            if (localIp == null)
            {
                localIp = CampUiFactory.Text(joinCard, "LocalIp",
                    "本机 IP：" + LanRoomService.GetLocalAddressText(),
                    14, CampUiFactory.Muted, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            }

            SetAnchors(title, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.95f));
            SetAnchors(nameLabel, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.80f));
            SetAnchors(nameInput, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.71f));
            SetAnchors(addressLabel, new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.57f));
            SetAnchors(addressInput, new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.48f));
            SetAnchors(createButton, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.34f));
            SetAnchors(joinButton, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.21f));
            SetAnchors(status, new Vector2(0.08f, 0.052f), new Vector2(0.92f, 0.095f));
            SetAnchors(localIp, new Vector2(0.08f, 0.008f), new Vector2(0.92f, 0.05f));
            status.fontSize = 15;
            localIp.fontSize = 14;

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("nameInput").objectReferenceValue = nameInput;
            serialized.FindProperty("addressInput").objectReferenceValue = addressInput;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("localIpText").objectReferenceValue = localIp;
            serialized.FindProperty("createButton").objectReferenceValue = createButton;
            serialized.FindProperty("joinButton").objectReferenceValue = joinButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StartPath);
        }

        private static void UpgradeLobbyScene()
        {
            Scene scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
            LobbySceneController controller =
                Object.FindObjectOfType<LobbySceneController>();
            if (controller == null)
                throw new System.InvalidOperationException("联机界面缺少 LobbySceneController。");

            RectTransform connectPanel = FindRect(controller.transform, "ConnectPanel");
            if (connectPanel != null)
                Object.DestroyImmediate(connectPanel.gameObject);
            RectTransform roomPanel = FindRect(controller.transform, "RoomPanel");
            if (roomPanel != null) roomPanel.gameObject.SetActive(true);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LobbyPath);
        }

        private static T FindComponent<T>(Transform root, string objectName)
            where T : Component
        {
            RectTransform rect = FindRect(root, objectName);
            return rect == null ? null : rect.GetComponent<T>();
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

        private static void SetAnchors(Component component,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            if (component == null) return;
            RectTransform rect = component.transform as RectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
