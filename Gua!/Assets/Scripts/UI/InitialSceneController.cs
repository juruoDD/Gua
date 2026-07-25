using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class InitialSceneController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button startButton;

        public Image BackgroundImage { get { return backgroundImage; } }

        private void Awake()
        {
            if (backgroundImage == null || startButton == null)
            {
                Debug.LogError("初始界面的 UI 引用不完整，请在 Inspector 中指定。");
                enabled = false;
                return;
            }
            startButton.onClick.AddListener(StartGame);
        }

        public void BuildLayoutForEditor()
        {
            CampUiFactory.EnsureEventSystem();
            Canvas canvas = CampUiFactory.CreateCanvas(transform);
            RectTransform background = CampUiFactory.Panel(canvas.transform, "BackgroundImage",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                CampUiFactory.Hex("#BFDFA5"));
            backgroundImage = background.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = false;

            RectTransform shade = CampUiFactory.Panel(canvas.transform, "ContentShade",
                new Vector2(0.27f, 0.16f), new Vector2(0.73f, 0.84f),
                Vector2.zero, Vector2.zero, new Color(0.97f, 0.98f, 0.88f, 0.92f), true);
            CampUiFactory.Text(shade, "EnglishTitle", "FROG CAMP", 24,
                CampUiFactory.Leaf, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.84f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(shade, "Title", "伪装者", 76,
                CampUiFactory.Deep, new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.73f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(shade, "Subtitle", "青蛙军营 · 局域网联机原型", 23,
                CampUiFactory.Muted, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.47f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            startButton = CampUiFactory.Button(shade, "StartGameButton", "开始游戏",
                new Vector2(0.25f, 0.16f), new Vector2(0.75f, 0.30f),
                Vector2.zero, Vector2.zero, null);
            CampUiFactory.Text(shade, "Hint", "点击进入", 17,
                CampUiFactory.Muted, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.13f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void StartGame()
        {
            SceneManager.LoadScene(CampScenes.Start);
        }
    }
}
