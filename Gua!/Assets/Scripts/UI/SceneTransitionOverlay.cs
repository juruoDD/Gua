using System.Collections;
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
        [SerializeField, Range(1f, 1.12f)] private float transitionScale = 1.045f;

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
            if (instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            instance.BeginLoad(sceneName);
        }

        private void BeginLoad(string sceneName)
        {
            if (loading) return;
            loading = true;
            StopAllCoroutines();
            StartCoroutine(CoverAndLoad(sceneName));
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
            SceneManager.LoadScene(sceneName);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}
