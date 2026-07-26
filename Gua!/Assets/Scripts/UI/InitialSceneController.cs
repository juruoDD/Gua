using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class InitialSceneController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button startButton;

        [Header("初始界面动效")]
        [SerializeField] private bool enableMotion = true;
        [SerializeField, Range(0.2f, 3f)] private float motionSpeed = 1f;
        [SerializeField, Range(0f, 12f)] private float backgroundDrift = 4f;
        [SerializeField, Range(1f, 1.05f)] private float backgroundZoom = 1.018f;
        [SerializeField, Range(0f, 12f)] private float titleFloat = 5f;
        [SerializeField, Range(0f, 0.06f)] private float buttonBreath = 0.018f;
        [SerializeField, Range(0f, 0.12f)] private float buttonHoverScale = 0.055f;

        private RectTransform backgroundRect;
        private RectTransform titleRect;
        private RectTransform buttonRect;
        private Vector2 backgroundBasePosition;
        private Vector2 titleBasePosition;
        private Vector2 buttonBasePosition;
        private Vector3 backgroundBaseScale;
        private Vector3 titleBaseScale;
        private Vector3 buttonBaseScale;
        private Canvas parentCanvas;
        private float motionStartedAt;
        private float buttonScale = 1f;
        private bool transitioning;

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

        private void Start()
        {
            backgroundRect = backgroundImage.rectTransform;
            buttonRect = startButton.transform as RectTransform;
            parentCanvas = startButton.GetComponentInParent<Canvas>();
            titleRect = FindRectTransform("标题", "Title");

            backgroundBasePosition = backgroundRect.anchoredPosition;
            backgroundBaseScale = backgroundRect.localScale;
            buttonBasePosition = buttonRect.anchoredPosition;
            buttonBaseScale = buttonRect.localScale;
            if (titleRect != null)
            {
                titleBasePosition = titleRect.anchoredPosition;
                titleBaseScale = titleRect.localScale;
            }
            motionStartedAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (!enableMotion || backgroundRect == null || buttonRect == null) return;

            float elapsed = (Time.unscaledTime - motionStartedAt) * motionSpeed;
            float entrance = Mathf.Clamp01(elapsed / 0.55f);
            entrance = 1f - Mathf.Pow(1f - entrance, 3f);

            backgroundRect.anchoredPosition = backgroundBasePosition + new Vector2(
                Mathf.Sin(elapsed * 0.31f) * backgroundDrift,
                Mathf.Cos(elapsed * 0.27f) * backgroundDrift * 0.55f);
            float backgroundPulse = backgroundZoom +
                Mathf.Sin(elapsed * 0.38f) * 0.0025f;
            backgroundRect.localScale = backgroundBaseScale * backgroundPulse;

            if (titleRect != null)
            {
                titleRect.anchoredPosition = titleBasePosition + new Vector2(0f,
                    Mathf.Sin(elapsed * 0.82f) * titleFloat - (1f - entrance) * 13f);
                float titlePulse = Mathf.Lerp(0.94f, 1f, entrance) *
                    (1f + Mathf.Sin(elapsed * 0.82f + 0.8f) * 0.012f);
                titleRect.localScale = titleBaseScale * titlePulse;
            }

            Camera eventCamera = parentCanvas != null &&
                parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera : null;
            bool hovered = RectTransformUtility.RectangleContainsScreenPoint(
                buttonRect, Input.mousePosition, eventCamera);
            bool pressed = hovered && Input.GetMouseButton(0);
            float targetScale = 1f + Mathf.Sin(elapsed * 2.25f) * buttonBreath;
            if (hovered) targetScale += buttonHoverScale;
            if (pressed || transitioning) targetScale -= 0.065f;
            targetScale *= Mathf.Lerp(0.90f, 1f, entrance);
            buttonScale = Mathf.Lerp(buttonScale, targetScale,
                1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
            buttonRect.anchoredPosition = buttonBasePosition +
                Vector2.up * ((1f - entrance) * -9f);
            buttonRect.localScale = buttonBaseScale * buttonScale;
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
            if (!transitioning) StartCoroutine(LoadStartScene());
        }

        private IEnumerator LoadStartScene()
        {
            transitioning = true;
            yield return new WaitForSecondsRealtime(0.14f);
            SceneTransitionOverlay.LoadScene(CampScenes.Start);
        }

        private RectTransform FindRectTransform(params string[] names)
        {
            RectTransform[] items = GetComponentsInChildren<RectTransform>(true);
            foreach (string targetName in names)
            {
                foreach (RectTransform item in items)
                {
                    if (item.name == targetName) return item;
                }
            }
            return null;
        }
    }
}
