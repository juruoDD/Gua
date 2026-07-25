using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class StartSceneController : MonoBehaviour
    {
        private InputField nameInput;
        private Text statusText;

        private void Awake()
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
                new Vector2(0.08f, 0.51f), new Vector2(0.92f, 0.63f),
                Vector2.zero, Vector2.zero, 12);
            nameInput.text = PlayerPrefs.GetString("frog_player_name", "");

            CampUiFactory.Button(joinCard, "CreateButton", "创建房间",
                new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.46f),
                Vector2.zero, Vector2.zero, CreateRoom);
            CampUiFactory.Button(joinCard, "BrowseButton", "查找 / 加入房间",
                new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.30f),
                Vector2.zero, Vector2.zero, OpenLobby, false);
            statusText = CampUiFactory.Text(joinCard, "Status", "准备连接局域网",
                17, CampUiFactory.Muted, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);

            CampUiFactory.Text(page, "Footer", "你一直看得见猎人，但猎人还没有看见你。",
                19, CampUiFactory.Leaf, new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.11f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void Update()
        {
            if (statusText != null) statusText.text = LanRoomService.Instance.Status;
        }

        private void CreateRoom()
        {
            SaveName();
            LanRoomService.Instance.HostRoom(nameInput.text);
            if (LanRoomService.Instance.CurrentRoom != null)
                SceneManager.LoadScene(CampScenes.Lobby);
        }

        private void OpenLobby()
        {
            SaveName();
            SceneManager.LoadScene(CampScenes.Lobby);
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
