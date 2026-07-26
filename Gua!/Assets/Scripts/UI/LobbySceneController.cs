using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class LobbySceneController : MonoBehaviour
    {
        [SerializeField] private Text[] playerLabels = new Text[4];
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private Text statusText;
        [SerializeField] private Text roomCodeText;
        [SerializeField] private Text roomCountText;
        [SerializeField] private Text roleHintText;
        [SerializeField] private Text readyLabel;
        [SerializeField] private Text startHintText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button officerButton;
        [SerializeField] private Button disguiserButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        private float nextRefreshTime;
        private bool returningToStart;

        private LanRoomService Service { get { return LanRoomService.Instance; } }

        private void Awake()
        {
            if (roomPanel == null || statusText == null || backButton == null ||
                officerButton == null ||
                disguiserButton == null || readyButton == null || startButton == null)
            {
                Debug.LogError("联机界面的 UI 引用不完整，请重新烘焙场景或在 Inspector 中指定。");
                enabled = false;
                return;
            }
            BindRuntimeEvents();
            Refresh();
        }

        public void BuildLayoutForEditor()
        {
            CampUiFactory.EnsureEventSystem();
            Canvas canvas = CampUiFactory.CreateCanvas(transform);
            RectTransform page = CampUiFactory.Panel(canvas.transform, "Page", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, CampUiFactory.Page);
            BuildHeader(page);
            BuildRoomPanel(page);
            statusText = CampUiFactory.Text(page, "GlobalStatus", "", 19,
                CampUiFactory.Muted, new Vector2(0.08f, 0.025f), new Vector2(0.92f, 0.075f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void BindRuntimeEvents()
        {
            backButton.onClick.AddListener(BackToStart);
            officerButton.onClick.AddListener(() => Service.SelectRole("officer"));
            disguiserButton.onClick.AddListener(() => Service.SelectRole("disguiser"));
            readyButton.onClick.AddListener(ToggleReady);
            startButton.onClick.AddListener(() => Service.RequestStart());
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void BuildHeader(RectTransform page)
        {
            CampUiFactory.Panel(page, "Header", new Vector2(0f, 0.88f), Vector2.one,
                Vector2.zero, Vector2.zero, CampUiFactory.Deep);
            CampUiFactory.Text(page, "HeaderTag", "LAN PROTOCOL  /  01", 19,
                CampUiFactory.Mint, new Vector2(0.07f, 0.91f), new Vector2(0.35f, 0.98f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            CampUiFactory.Text(page, "HeaderTitle", "军营联机准备室", 36,
                CampUiFactory.White, new Vector2(0.34f, 0.89f), new Vector2(0.66f, 0.99f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            backButton = CampUiFactory.Button(page, "BackButton", "返回",
                new Vector2(0.83f, 0.91f), new Vector2(0.93f, 0.975f),
                Vector2.zero, Vector2.zero, null, false);
        }

        private void BuildRoomPanel(RectTransform page)
        {
            RectTransform panel = CampUiFactory.Panel(page, "RoomPanel",
                new Vector2(0.06f, 0.11f), new Vector2(0.94f, 0.84f),
                Vector2.zero, Vector2.zero, CampUiFactory.Paper, true);
            roomPanel = panel.gameObject;

            roomCodeText = CampUiFactory.Text(panel, "RoomCode", "房间 ----", 34,
                CampUiFactory.Deep, new Vector2(0.05f, 0.83f), new Vector2(0.38f, 0.95f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            roomCountText = CampUiFactory.Text(panel, "RoomCount", "0 / 4",
                24, CampUiFactory.Leaf, new Vector2(0.68f, 0.84f), new Vector2(0.94f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleRight, true);

            RectTransform roster = CampUiFactory.Panel(panel, "Roster",
                new Vector2(0.05f, 0.17f), new Vector2(0.59f, 0.80f),
                Vector2.zero, Vector2.zero, CampUiFactory.Mint, true);
            CampUiFactory.Text(roster, "RosterTitle", "人员频道", 25,
                CampUiFactory.Deep, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            playerLabels = new Text[4];
            for (int index = 0; index < 4; index++)
            {
                float top = 0.80f - index * 0.18f;
                RectTransform slot = CampUiFactory.Panel(roster, "PlayerSlot" + index,
                    new Vector2(0.05f, top - 0.14f), new Vector2(0.95f, top),
                    Vector2.zero, Vector2.zero, index % 2 == 0
                        ? CampUiFactory.White : CampUiFactory.Paper, true);
                playerLabels[index] = CampUiFactory.Text(slot, "Player", "空位",
                    21, CampUiFactory.Muted, Vector2.zero, Vector2.one,
                    new Vector2(18f, 0f), new Vector2(-18f, 0f), TextAnchor.MiddleLeft,
                    index == 0);
            }

            RectTransform preparation = CampUiFactory.Panel(panel, "Preparation",
                new Vector2(0.63f, 0.17f), new Vector2(0.95f, 0.80f),
                Vector2.zero, Vector2.zero, CampUiFactory.White, true);
            CampUiFactory.Text(preparation, "Title", "身份准备", 29,
                CampUiFactory.Deep, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            officerButton = CampUiFactory.Button(preparation, "OfficerButton", "军官  ·  1 人",
                new Vector2(0.08f, 0.65f), new Vector2(0.92f, 0.78f),
                Vector2.zero, Vector2.zero, null, false);
            disguiserButton = CampUiFactory.Button(preparation, "DisguiserButton",
                "伪装者  ·  最多 3 人",
                new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.62f),
                Vector2.zero, Vector2.zero, null, false);
            roleHintText = CampUiFactory.Text(preparation, "RoleHint", "请选择身份",
                18, CampUiFactory.Muted, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.48f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            readyButton = CampUiFactory.Button(preparation, "ReadyButton", "准备",
                new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.37f),
                Vector2.zero, Vector2.zero, null);
            readyLabel = CampUiFactory.ButtonLabel(readyButton);
            startButton = CampUiFactory.Button(preparation, "StartButton", "开始游戏",
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.21f),
                Vector2.zero, Vector2.zero, null);
            startHintText = CampUiFactory.Text(panel, "StartHint", "",
                18, CampUiFactory.Muted, new Vector2(0.63f, 0.05f), new Vector2(0.95f, 0.14f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void Refresh()
        {
            bool inRoom = Service.CurrentRoom != null;
            roomPanel.SetActive(inRoom);
            statusText.text = Service.Status;
            if (!inRoom)
            {
                if (!returningToStart)
                {
                    returningToStart = true;
                    SceneTransitionOverlay.LoadScene(CampScenes.Start);
                }
                return;
            }

            RoomStateData room = Service.CurrentRoom;
            roomCodeText.text = "房间代码  " + room.code;
            roomCountText.text = "在线  " + room.players.Count + " / 4";
            for (int index = 0; index < playerLabels.Length; index++)
            {
                if (index >= room.players.Count)
                {
                    playerLabels[index].text = "空位";
                    playerLabels[index].color = CampUiFactory.Muted;
                    continue;
                }
                RoomPlayerData player = room.players[index];
                string role = player.role == "officer" ? "军官"
                    : player.role == "disguiser" ? "伪装者" : "未选择";
                playerLabels[index].text = (player.host ? "★ " : "● ") + player.name +
                    "     " + role + "     " + (player.ready ? "已准备" : "准备中");
                playerLabels[index].color = player.ready
                    ? CampUiFactory.Leaf : CampUiFactory.Deep;
            }

            RoomPlayerData localPlayer = Service.GetLocalPlayer();
            string localRole = localPlayer == null ? "" : localPlayer.role;
            roleHintText.text = localRole == "officer" ? "当前身份：军官"
                : localRole == "disguiser" ? "当前身份：伪装者" : "请选择身份后准备";
            readyButton.interactable = localPlayer != null && !string.IsNullOrEmpty(localRole);
            readyLabel.text = localPlayer != null && localPlayer.ready ? "取消准备" : "准备";

            string reason;
            bool canStart = Service.CanStart(out reason);
            startButton.gameObject.SetActive(Service.IsHost);
            startButton.interactable = canStart;
            startHintText.text = Service.IsHost ? reason : "等待房主开始游戏";
        }

        private void ToggleReady()
        {
            RoomPlayerData player = Service.GetLocalPlayer();
            if (player != null) Service.SetReady(!player.ready);
        }

        private void BackToStart()
        {
            Service.LeaveRoom();
            SceneTransitionOverlay.LoadScene(CampScenes.Start);
        }

    }
}
