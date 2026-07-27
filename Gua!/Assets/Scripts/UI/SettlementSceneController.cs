using System.Collections.Generic;
using System.Linq;
using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class SettlementSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform[] lilyPads = new RectTransform[4];
        [SerializeField] private RawImage[] frogImages = new RawImage[4];
        [SerializeField] private Text[] playerNameTexts = new Text[4];
        [SerializeField] private Text[] resultTexts = new Text[4];
        [SerializeField] private SettlementResultEffect[] resultEffects =
            new SettlementResultEffect[4];
        [SerializeField] private Button backButton;
        [SerializeField] private Texture2D greenSalute;
        [SerializeField] private Texture2D greenDeath;
        [SerializeField] private Texture2D pinkFallback;
        [SerializeField] private Color victoryTextColor =
            new Color32(255, 205, 62, 255);
        [SerializeField] private Color defeatTextColor =
            new Color32(190, 58, 74, 255);

        private readonly List<AnimatedResultFrog> animatedFrogs =
            new List<AnimatedResultFrog>();

        private void Awake()
        {
            backButton.onClick.AddListener(ReturnToStart);
            BuildResults();
        }

        private void Update()
        {
            foreach (AnimatedResultFrog frog in animatedFrogs)
                frog.UpdateFrame(Time.unscaledTime);
        }

        private void BuildResults()
        {
            RoomStateData room = LanRoomService.Instance.CurrentRoom;
            GameStateData game = room == null ? null : room.game;
            List<GameActorData> players = game == null
                ? new List<GameActorData>()
                : game.players.Where(player => !player.npc).ToList();

            for (int index = 0; index < lilyPads.Length; index++)
            {
                RectTransform lily = lilyPads[index];
                if (lily == null) continue;
                if (index >= players.Count)
                {
                    frogImages[index].enabled = false;
                    playerNameTexts[index].text = "";
                    resultTexts[index].text = "";
                    resultEffects[index].gameObject.SetActive(false);
                    continue;
                }

                GameActorData player = players[index];
                bool won = game != null && player.role == game.winnerRole;
                ConfigureFrog(index, player.role, won);
                playerNameTexts[index].text = player.name;
                resultTexts[index].text = won ? "胜利！" : "失败！";
                resultTexts[index].color = won
                    ? victoryTextColor : defeatTextColor;
                resultEffects[index].gameObject.SetActive(true);
                resultEffects[index].SetVictory(won);
            }
        }

        private void ConfigureFrog(int index, string role, bool won)
        {
            bool officer = role == "officer";
            Texture2D texture = officer ? pinkFallback
                : won ? greenSalute : greenDeath;
            int frameCount = officer ? 6 : won ? 8 : 6;
            Vector2 size = officer
                ? new Vector2(132f, 132f) : new Vector2(132f, 264f);
            Vector2 position = officer
                ? new Vector2(0f, 55f) : new Vector2(0f, 118f);

            RawImage image = frogImages[index];
            image.enabled = true;
            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            image.texture = texture;
            animatedFrogs.Add(new AnimatedResultFrog(image, frameCount,
                !officer, Time.unscaledTime));
        }

        private void ReturnToStart()
        {
            LanRoomService.Instance.LeaveRoom();
            SceneTransitionOverlay.LoadScene(CampScenes.Start);
        }

        private sealed class AnimatedResultFrog
        {
            private readonly RawImage image;
            private readonly int frameCount;
            private readonly bool oneShot;
            private readonly float startedAt;

            public AnimatedResultFrog(RawImage image, int frameCount,
                bool oneShot, float startedAt)
            {
                this.image = image;
                this.frameCount = frameCount;
                this.oneShot = oneShot;
                this.startedAt = startedAt;
            }

            public void UpdateFrame(float time)
            {
                int elapsedFrame = Mathf.FloorToInt(
                    Mathf.Max(0f, time - startedAt) * 8f);
                int frame = oneShot
                    ? Mathf.Min(frameCount - 1, elapsedFrame)
                    : elapsedFrame % frameCount;
                image.uvRect = new Rect(frame / (float)frameCount, 0f,
                    1f / frameCount, 1f);
            }
        }
    }
}
