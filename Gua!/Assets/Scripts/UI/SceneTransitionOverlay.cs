using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.UI
{
    public sealed class SceneTransitionOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform overlayRect;
        [SerializeField, Range(0.1f, 1f)] private float revealDuration = 0.34f;
        [SerializeField, Range(0.1f, 1f)] private float coverDuration = 0.28f;
        [SerializeField, Range(0f, 1.5f)] private float coveredHoldDuration = 0.35f;
        [SerializeField, Range(1f, 1.12f)] private float transitionScale = 1.045f;
        [SerializeField] private TMP_FontAsset transitionFont;
        [SerializeField] private TextMeshProUGUI transitionLabel;
        [Header("新手试玩过渡")]
        [SerializeField] private string tutorialTransitionText =
            "进入新手试玩";
        [SerializeField, Range(28f, 100f)]
        private float transitionFontSize = 62f;
        [SerializeField] private Color transitionTextColor = Color.white;
        [SerializeField] private Color transitionOutlineColor =
            new Color(0.06f, 0.15f, 0.17f, 0.86f);

        private static SceneTransitionOverlay instance;
        private bool loading;

        public void Configure(CanvasGroup group, RectTransform rect)
        {
            canvasGroup = group;
            overlayRect = rect;
        }

        private void Awake()
        {
            instance = this;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (overlayRect == null) overlayRect = transform as RectTransform;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            overlayRect.localScale = Vector3.one * transitionScale;
        }

        private void Start()
        {
            StartCoroutine(Reveal());
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void LoadScene(string sceneName)
        {
            LoadScene(sceneName, null);
        }

        public static void LoadScene(string sceneName, string transitionText)
        {
            if (instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            instance.BeginLoad(sceneName, transitionText);
        }

        public static void LoadTutorialScene(string sceneName)
        {
            if (instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            instance.BeginLoad(
                sceneName, instance.tutorialTransitionText);
        }

        private void BeginLoad(string sceneName, string transitionText)
        {
            if (loading) return;
            loading = true;
            StopAllCoroutines();
            PrepareLabel(transitionText);
            StartCoroutine(CoverAndLoad(sceneName));
        }

        private void PrepareLabel(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (transitionLabel != null)
                    transitionLabel.gameObject.SetActive(false);
                return;
            }
            if (transitionLabel == null)
            {
                GameObject labelObject = new GameObject(
                    "TransitionLabel", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                RectTransform labelRect =
                    labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(overlayRect, false);
                labelRect.anchorMin = new Vector2(0.12f, 0.38f);
                labelRect.anchorMax = new Vector2(0.88f, 0.62f);
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                transitionLabel =
                    labelObject.GetComponent<TextMeshProUGUI>();
                transitionLabel.font = transitionFont != null
                    ? transitionFont : TMP_Settings.defaultFontAsset;
                transitionLabel.fontSize = transitionFontSize;
                transitionLabel.fontStyle = FontStyles.Bold;
                transitionLabel.alignment = TextAlignmentOptions.Center;
                transitionLabel.color = transitionTextColor;
                transitionLabel.outlineColor = transitionOutlineColor;
                transitionLabel.outlineWidth = 0.16f;
                transitionLabel.raycastTarget = false;
            }
            transitionLabel.fontSize = transitionFontSize;
            transitionLabel.color = transitionTextColor;
            transitionLabel.outlineColor = transitionOutlineColor;
            transitionLabel.text = text;
            transitionLabel.gameObject.SetActive(true);
            transitionLabel.transform.SetAsLastSibling();
        }

        private IEnumerator Reveal()
        {
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Smooth(Mathf.Clamp01(elapsed / revealDuration));
                canvasGroup.alpha = 1f - progress;
                overlayRect.localScale = Vector3.one *
                    Mathf.Lerp(transitionScale, 1f, progress);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            overlayRect.localScale = Vector3.one;
        }

        private IEnumerator CoverAndLoad(string sceneName)
        {
            canvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < coverDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Smooth(Mathf.Clamp01(elapsed / coverDuration));
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                overlayRect.localScale = Vector3.one *
                    Mathf.Lerp(1f, transitionScale, progress);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            if (coveredHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(coveredHoldDuration);
            SceneManager.LoadScene(sceneName);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}
