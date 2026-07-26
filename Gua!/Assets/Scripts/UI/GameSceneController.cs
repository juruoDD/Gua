using System.Collections.Generic;
using System.Linq;
using FrogCamp.Gameplay;
using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform actorLayer;
        [SerializeField] private Text roomText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text announcementText;
        [SerializeField] private Button exitButton;
        [SerializeField] private FrogAnimationSet greenAnimations = new FrogAnimationSet();
        [SerializeField] private FrogAnimationSet pinkAnimations = new FrogAnimationSet();

        private readonly Dictionary<string, FrogActorView> actorViews =
            new Dictionary<string, FrogActorView>();
        private float nextInputTime;
        private int lastAnnouncementId;

        private void Awake()
        {
            if (actorLayer == null || roomText == null || statusText == null ||
                announcementText == null || exitButton == null)
            {
                Debug.LogError("游戏界面的 UI 引用不完整，请重新烘焙场景。");
                enabled = false;
                return;
            }
            exitButton.onClick.AddListener(ExitGame);
            announcementText.gameObject.SetActive(false);
        }

        private void Update()
        {
            HandleInput();
            RefreshActors();
        }

        public void BuildLayoutForEditor()
        {
#if UNITY_EDITOR
            greenAnimations.SetTextures(
                LoadFrogTexture("待机"), LoadFrogTexture("小跳"), LoadFrogTexture("大跳"),
                LoadFrogTexture("伸左手"), LoadFrogTexture("伸右手"),
                LoadFrogTexture("伸左腿"), LoadFrogTexture("伸右腿"),
                LoadFrogTexture("张嘴"), LoadFrogTexture("吐舌"), null,
                LoadFrogTexture("敬礼"));
            pinkAnimations.SetTextures(
                LoadFrogTexture("粉色待机"), LoadFrogTexture("粉色小跳"),
                LoadFrogTexture("粉色大跳"), null, null, null, null,
                LoadFrogTexture("粉色张嘴"), LoadFrogTexture("粉色吐舌"),
                null, null);
#endif
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
            AddFlag(map);

            actorLayer = CampUiFactory.Panel(map, "ActorLayer", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            actorLayer.GetComponent<Image>().raycastTarget = false;

            RectTransform networkCard = CampUiFactory.Panel(map, "NetworkCard",
                new Vector2(0.02f, 0.89f), new Vector2(0.30f, 0.98f),
                Vector2.zero, Vector2.zero, new Color(CampUiFactory.Deep.r,
                    CampUiFactory.Deep.g, CampUiFactory.Deep.b, .9f), true);
            roomText = CampUiFactory.Text(networkCard, "Room", "房间 ----", 20,
                CampUiFactory.White, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            statusText = CampUiFactory.Text(networkCard, "Status", "联机状态确认中",
                15, CampUiFactory.Mint, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.48f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            exitButton = CampUiFactory.Button(map, "ExitButton", "退出",
                new Vector2(0.90f, 0.92f), new Vector2(0.98f, 0.975f),
                Vector2.zero, Vector2.zero, null, false);
            announcementText = CampUiFactory.Text(map, "Announcement", "",
                27, CampUiFactory.White, new Vector2(0.31f, 0.89f),
                new Vector2(0.69f, 0.97f), Vector2.zero, Vector2.zero,
                TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(map, "Controls",
                "WASD / 方向键 移动   空格 大跳   H 呱叫   J 吐舌   K 敬礼/吹哨   1-4 伸展",
                15, CampUiFactory.Leaf, new Vector2(0.18f, 0.01f),
                new Vector2(0.82f, 0.055f), Vector2.zero, Vector2.zero,
                TextAnchor.MiddleCenter, true);
        }

        private void HandleInput()
        {
            LanRoomService service = LanRoomService.Instance;
            if (service.CurrentRoom == null || !service.CurrentRoom.inGame) return;
            if (Time.unscaledTime >= nextInputTime)
            {
                nextInputTime = Time.unscaledTime + 0.05f;
                float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                          (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                float y = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f) -
                          (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);
                service.SetGameInput(x, y);
            }
            if (Input.GetKeyDown(KeyCode.Space)) service.TriggerGameAction("jump");
            if (Input.GetKeyDown(KeyCode.Alpha1)) service.TriggerGameAction("armRight");
            if (Input.GetKeyDown(KeyCode.Alpha2)) service.TriggerGameAction("armLeft");
            if (Input.GetKeyDown(KeyCode.Alpha3)) service.TriggerGameAction("legLeft");
            if (Input.GetKeyDown(KeyCode.Alpha4)) service.TriggerGameAction("legRight");
            if (Input.GetKeyDown(KeyCode.H)) service.TriggerGameAction("croak");
            if (Input.GetKeyDown(KeyCode.J)) service.TriggerGameAction("tongue");
            if (Input.GetKeyDown(KeyCode.K))
            {
                RoomPlayerData localPlayer = service.GetLocalPlayer();
                service.TriggerGameAction(localPlayer != null &&
                    localPlayer.role == "officer" ? "whistle" : "salute");
            }
        }

        private void RefreshActors()
        {
            LanRoomService service = LanRoomService.Instance;
            RoomStateData room = service.CurrentRoom;
            roomText.text = room == null ? "离线" : "房间 " + room.code;
            statusText.text = room == null ? "当前未连接房间" : service.Status;
            if (room == null || room.game == null) return;

            List<GameActorData> actors = room.game.npcs.Concat(room.game.players).ToList();
            HashSet<string> activeIds = new HashSet<string>();
            foreach (GameActorData actor in actors)
            {
                activeIds.Add(actor.id);
                FrogActorView view;
                if (!actorViews.TryGetValue(actor.id, out view))
                {
                    view = FrogActorView.Create(actorLayer, actor, greenAnimations,
                        pinkAnimations);
                    actorViews.Add(actor.id, view);
                }
                view.Apply(actor);
            }
            foreach (string id in actorViews.Keys.Where(id => !activeIds.Contains(id)).ToArray())
            {
                Destroy(actorViews[id].gameObject);
                actorViews.Remove(id);
            }
            foreach (FrogActorView view in actorViews.Values.OrderBy(view => view.SortY))
                view.transform.SetAsLastSibling();

            if (room.game.announcementId != lastAnnouncementId)
            {
                lastAnnouncementId = room.game.announcementId;
                StopAllCoroutines();
                StartCoroutine(ShowAnnouncement(room.game.announcement));
            }
        }

        private System.Collections.IEnumerator ShowAnnouncement(string message)
        {
            announcementText.text = message;
            announcementText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.8f);
            announcementText.gameObject.SetActive(false);
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

#if UNITY_EDITOR
        private static Texture2D LoadFrogTexture(string fileName)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Frog/" + fileName + ".png");
        }
#endif

        private static void AddFlag(RectTransform map)
        {
            RectTransform pole = CampUiFactory.Panel(map, "FlagPole",
                new Vector2(.497f, .53f), new Vector2(.502f, .70f),
                Vector2.zero, Vector2.zero, CampUiFactory.Deep);
            CampUiFactory.Panel(pole, "Flag", new Vector2(1f, .64f),
                new Vector2(7f, 1f), Vector2.zero, Vector2.zero,
                CampUiFactory.Hex("#F18A78"));
        }

        private void ExitGame()
        {
            LanRoomService.Instance.LeaveRoom();
            SceneManager.LoadScene(CampScenes.Start);
        }
    }
}
