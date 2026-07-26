using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class StartSceneController : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private InputField addressInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Text localIpText;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;
        private bool waitingForJoin;
        private string localStatus;

        private void Awake()
        {
            if (nameInput == null || addressInput == null ||
                statusText == null || localIpText == null ||
                createButton == null || joinButton == null)
            {
                Debug.LogError("开始界面的 UI 引用不完整，请重新烘焙场景或在 Inspector 中指定。");
                enabled = false;
                return;
            }

            nameInput.text = PlayerPrefs.GetString("frog_player_name", "");
            addressInput.text = PlayerPrefs.GetString("frog_host_address", "");
            localIpText.text = "本机 IP：" + LanRoomService.GetLocalAddressText();
            createButton.onClick.AddListener(CreateRoom);
            joinButton.onClick.AddListener(JoinRoom);
            addressInput.onValueChanged.AddListener(_ => localStatus = "");
        }

        private void Update()
        {
            statusText.text = string.IsNullOrEmpty(localStatus)
                ? LanRoomService.Instance.Status : localStatus;
            if (waitingForJoin && LanRoomService.Instance.CurrentRoom != null)
            {
                waitingForJoin = false;
                SceneTransitionOverlay.LoadScene(CampScenes.Lobby);
            }
        }

        public void BuildLayoutForEditor()
        {
            CampUiFactory.EnsureEventSystem();
            Canvas canvas = CampUiFactory.CreateCanvas(transform);
            RectTransform page = CampUiFactory.Panel(canvas.transform, "Page", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, CampUiFactory.Page);

            CampUiFactory.Panel(page, "TopBand", new Vector2(0f, 0.89f), Vector2.one,
                Vector2.zero, Vector2.zero, CampUiFactory.Deep);
            CampUiFactory.Text(page, "Protocol", "LOCAL MULTIPLAYER  ·  2D", 22,
                CampUiFactory.Mint, new Vector2(0.08f, 0.91f), new Vector2(0.5f, 0.98f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            CampUiFactory.Text(page, "GameName", "伪装者", 42, CampUiFactory.White,
                new Vector2(0.72f, 0.90f), new Vector2(0.92f, 0.99f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleRight, true);

            RectTransform card = CampUiFactory.Panel(page, "WelcomeCard",
                new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.82f),
                Vector2.zero, Vector2.zero, CampUiFactory.Paper, true);
            CampUiFactory.Panel(card, "SideMark", Vector2.zero, new Vector2(0.018f, 1f),
                Vector2.zero, Vector2.zero, CampUiFactory.Accent);
            CampUiFactory.Text(card, "Eyebrow", "WELCOME TO THE CAMP", 20,
                CampUiFactory.Leaf, new Vector2(0.08f, 0.76f), new Vector2(0.54f, 0.88f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            CampUiFactory.Text(card, "Title", "欢迎来到\n青蛙军营", 66,
                CampUiFactory.Deep, new Vector2(0.08f, 0.40f), new Vector2(0.54f, 0.76f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            CampUiFactory.Text(card, "Description",
                "建立一个最多 4 人的局域网小队。\n选择军官或伪装者，确认所有设备都已在线。",
                25, CampUiFactory.Muted, new Vector2(0.08f, 0.21f), new Vector2(0.53f, 0.40f),
                Vector2.zero, Vector2.zero, TextAnchor.UpperLeft);

            RectTransform joinCard = CampUiFactory.Panel(card, "JoinCard",
                new Vector2(0.58f, 0.14f), new Vector2(0.93f, 0.86f),
                Vector2.zero, Vector2.zero, CampUiFactory.Mint, true);
            CampUiFactory.Text(joinCard, "JoinTitle", "进入通讯频道", 31,
                CampUiFactory.Deep, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.90f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            CampUiFactory.Text(joinCard, "NameLabel", "行动代号", 20,
                CampUiFactory.Leaf, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.74f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            nameInput = CampUiFactory.Input(joinCard, "NameInput", "输入你的名字",
                new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.69f),
                Vector2.zero, Vector2.zero, 12);
            CampUiFactory.Text(joinCard, "AddressLabel", "房主 IP", 20,
                CampUiFactory.Leaf, new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.55f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            addressInput = CampUiFactory.Input(joinCard, "AddressInput", "例如 192.168.1.20",
                new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.46f),
                Vector2.zero, Vector2.zero, 45);
            createButton = CampUiFactory.Button(joinCard, "CreateButton", "创建房间",
                new Vector2(0.08f, 0.21f), new Vector2(0.48f, 0.32f),
                Vector2.zero, Vector2.zero, null);
            joinButton = CampUiFactory.Button(joinCard, "JoinButton", "加入房间",
                new Vector2(0.52f, 0.21f), new Vector2(0.92f, 0.32f),
                Vector2.zero, Vector2.zero, null, false);
            statusText = CampUiFactory.Text(joinCard, "Status", "等待操作",
                16, CampUiFactory.Muted, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.18f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            localIpText = CampUiFactory.Text(joinCard, "LocalIp", "本机 IP：检测中",
                15, CampUiFactory.Muted, new Vector2(0.08f, 0.025f), new Vector2(0.92f, 0.09f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            CampUiFactory.Text(page, "Footer", "你一直看得见猎人，但猎人还没有看见你。",
                19, CampUiFactory.Leaf, new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.11f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void CreateRoom()
        {
            SaveName();
            LanRoomService.Instance.HostRoom(nameInput.text);
            if (LanRoomService.Instance.CurrentRoom != null)
                SceneTransitionOverlay.LoadScene(CampScenes.Lobby);
        }

        private void JoinRoom()
        {
            SaveName();
            string address = addressInput.text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                localStatus = "请输入房主 IP";
                return;
            }
            PlayerPrefs.SetString("frog_host_address", address);
            PlayerPrefs.Save();
            localStatus = "";
            waitingForJoin = true;
            LanRoomService.Instance.JoinAddress(nameInput.text, address);
        }

        private void SaveName()
        {
            string playerName = string.IsNullOrWhiteSpace(nameInput.text)
                ? "青蛙玩家" : nameInput.text.Trim();
            nameInput.text = playerName;
            PlayerPrefs.SetString("frog_player_name", playerName);
            PlayerPrefs.Save();
        }
    }
}
