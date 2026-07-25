using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class GameSceneController : MonoBehaviour
    {
        private readonly Text[] playerLines = new Text[4];
        private Text roomText;
        private Text statusText;
        private float nextRefreshTime;

        private void Awake()
        {
            CampUiFactory.EnsureEventSystem();
            Canvas canvas = CampUiFactory.CreateCanvas(transform);
            RectTransform map = CampUiFactory.Panel(canvas.transform, "CampMap", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, CampUiFactory.Page);

            CampUiFactory.Panel(map, "HorizontalRoad", new Vector2(0f, 0.44f),
                new Vector2(1f, 0.56f), Vector2.zero, Vector2.zero, CampUiFactory.Paper);
            CampUiFactory.Panel(map, "VerticalRoad", new Vector2(0.46f, 0f),
                new Vector2(0.54f, 1f), Vector2.zero, Vector2.zero, CampUiFactory.Paper);
            AddBuilding(map, "营帐 A", new Vector2(0.08f, 0.68f), new Vector2(0.28f, 0.86f));
            AddBuilding(map, "营帐 B", new Vector2(0.72f, 0.68f), new Vector2(0.92f, 0.86f));
            AddBuilding(map, "仓库", new Vector2(0.08f, 0.14f), new Vector2(0.28f, 0.32f));
            AddBuilding(map, "食堂", new Vector2(0.72f, 0.14f), new Vector2(0.92f, 0.32f));

            RectTransform notice = CampUiFactory.Panel(map, "Notice",
                new Vector2(0.31f, 0.31f), new Vector2(0.69f, 0.69f),
                Vector2.zero, Vector2.zero, CampUiFactory.White, true);
            CampUiFactory.Text(notice, "Tag", "POSITION SYNC TEST", 18,
                CampUiFactory.Leaf, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.90f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(notice, "Title", "游戏场景布局占位", 38,
                CampUiFactory.Deep, new Vector2(0.08f, 0.53f), new Vector2(0.92f, 0.76f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(notice, "Body",
                "房间状态与场景切换已经同步。\n移动、AI、技能和碰撞将在后续模块接入。",
                22, CampUiFactory.Muted, new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.52f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            CampUiFactory.Button(notice, "ExitButton", "退出测试",
                new Vector2(0.26f, 0.09f), new Vector2(0.74f, 0.23f),
                Vector2.zero, Vector2.zero, ExitGame, false);

            RectTransform networkCard = CampUiFactory.Panel(map, "NetworkCard",
                new Vector2(0.025f, 0.88f), new Vector2(0.35f, 0.975f),
                Vector2.zero, Vector2.zero, CampUiFactory.Deep, true);
            roomText = CampUiFactory.Text(networkCard, "Room", "房间 ----", 21,
                CampUiFactory.White, new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            statusText = CampUiFactory.Text(networkCard, "Status", "联机状态确认中",
                16, CampUiFactory.Mint, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.48f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);

            RectTransform players = CampUiFactory.Panel(map, "PlayerCard",
                new Vector2(0.33f, 0.025f), new Vector2(0.67f, 0.285f),
                Vector2.zero, Vector2.zero, CampUiFactory.Deep, true);
            CampUiFactory.Text(players, "Title", "在线成员", 21, CampUiFactory.White,
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            for (int index = 0; index < 4; index++)
            {
                float top = 0.75f - index * 0.17f;
                playerLines[index] = CampUiFactory.Text(players, "Player" + index, "—",
                    17, CampUiFactory.Mint, new Vector2(0.08f, top - 0.14f),
                    new Vector2(0.92f, top), Vector2.zero, Vector2.zero,
                    TextAnchor.MiddleLeft);
            }
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            LanRoomService service = LanRoomService.Instance;
            RoomStateData room = service.CurrentRoom;
            roomText.text = room == null ? "离线布局预览" : "房间 " + room.code;
            statusText.text = room == null ? "当前未连接房间" : service.Status;
            for (int index = 0; index < playerLines.Length; index++)
            {
                if (room == null || index >= room.players.Count)
                {
                    playerLines[index].text = "○ 等待成员";
                    continue;
                }
                RoomPlayerData player = room.players[index];
                string role = player.role == "officer" ? "军官"
                    : player.role == "disguiser" ? "伪装者" : "未选择";
                playerLines[index].text = "● " + player.name + "  /  " + role;
            }
        }

        private static void AddBuilding(RectTransform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform building = CampUiFactory.Panel(parent, label, anchorMin, anchorMax,
                Vector2.zero, Vector2.zero, CampUiFactory.Leaf, true);
            CampUiFactory.Panel(building, "Roof", new Vector2(0.08f, 0.78f),
                new Vector2(0.92f, 1f), Vector2.zero, Vector2.zero, CampUiFactory.Deep);
            CampUiFactory.Text(building, "Label", label, 24, CampUiFactory.White,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.78f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
        }

        private void ExitGame()
        {
            LanRoomService.Instance.LeaveRoom();
            SceneManager.LoadScene(CampScenes.Start);
        }
    }
}
