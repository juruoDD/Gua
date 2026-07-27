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
        [SerializeField] private Texture2D pinkSalute;
        [SerializeField] private Texture2D pinkDeath;
        [SerializeField] private Color victoryTextColor =
            new Color32(255, 205, 62, 255);
        [SerializeField] private Color defeatTextColor =
            new Color32(190, 58, 74, 255);

        private readonly List<AnimatedResultFrog> animatedFrogs =
            new List<AnimatedResultFrog>();

        private void Awake()
        {
            if (backButton == null)
                backButton = GetComponentInChildren<Button>(true);
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ReturnToStart);
                backButton.onClick.AddListener(ReturnToStart);
            }
            else
            {
                Debug.LogError("Settlement scene is missing its back button.");
            }
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
            Texture2D texture = officer
                ? (won ? pinkSalute : pinkDeath)
                : (won ? greenSalute : greenDeath);
            int frameCount = won ? 8 : 6;
            Vector2 size = new Vector2(132f, 264f);
            Vector2 position = new Vector2(0f, 118f);

            RawImage image = frogImages[index];
            image.enabled = true;
            FrogCamp.Gameplay.SoftSilhouetteShadow silhouetteShadow =
                image.GetComponent<FrogCamp.Gameplay.SoftSilhouetteShadow>();
            if (!officer && silhouetteShadow == null)
                silhouetteShadow =
                    image.gameObject.AddComponent<
                        FrogCamp.Gameplay.SoftSilhouetteShadow>();
            if (silhouetteShadow != null)
            {
                silhouetteShadow.enabled = !officer;
                if (!officer)
                    silhouetteShadow.Configure(
                        new Color(0.07f, 0.18f, 0.16f, 0.24f), 2.6f);
            }
            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            image.texture = texture;
            animatedFrogs.Add(new AnimatedResultFrog(image, texture,
                won ? "salute" : "death", frameCount, Time.unscaledTime));
        }

        private void ReturnToStart()
        {
            // Room cleanup notifies listeners and must never be allowed to
            // prevent the navigation requested by this button.
            try
            {
                if (LanRoomService.Instance != null)
                    LanRoomService.Instance.LeaveRoom();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                SceneTransitionOverlay.LoadScene(CampScenes.Start);
            }
        }

        private sealed class AnimatedResultFrog
        {
            private readonly RawImage image;
            private readonly Texture2D texture;
            private readonly string state;
            private readonly int frameCount;
            private readonly float startedAt;

            public AnimatedResultFrog(RawImage image, Texture2D texture,
                string state, int frameCount, float startedAt)
            {
                this.image = image;
                this.texture = texture;
                this.state = state;
                this.frameCount = frameCount;
                this.startedAt = startedAt;
            }

            public void UpdateFrame(float time)
            {
                int elapsedFrame = Mathf.FloorToInt(
                    Mathf.Max(0f, time - startedAt) * 8f);
                int frame = elapsedFrame % frameCount;
                image.uvRect = FrogCamp.Gameplay.FrogAnimationSet.GetFrameUv(
                    state, texture, frame, frameCount);
            }
        }
    }
}
