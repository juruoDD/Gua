using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    [System.Serializable]
    public sealed class GameEventPresentationSettings
    {
        [Header("文字排版")]
        [Range(48, 180)] public int baseFontSize = 132;
        [Range(32, 120)] public int minimumFontSize = 58;
        [Range(64, 220)] public int maximumFontSize = 152;
        [Range(-12f, 32f)] public float characterSpacing = 4f;
        [Range(1f, 14f)] public float outlineSize = 7f;
        [Range(0f, 24f)] public float shadowDistance = 13f;

        [Header("文案")]
        public string progressTemplate = "任务已完成{0}%！";
        public string capturedTemplate = "{0}被抓获！";
        public string disguiserVictoryText = "任务已全部完成！";
        public string officerVictoryText = "捣蛋呱全军覆没！";
        public string goText = "GO!";

        [Header("显示时长")]
        [Range(0.5f, 6f)] public float normalDuration = 2.6f;
        [Range(1f, 8f)] public float endingDuration = 3.2f;
        [Range(0.2f, 2f)] public float goDuration = 0.7f;

        [Header("任务进度配色")]
        public Color progressTextColor =
            new Color(1f, 0.82f, 0.26f, 0.88f);
        public Color progressParticleColor =
            new Color(0.18f, 0.88f, 1f, 0.96f);

        [Header("抓获配色")]
        public Color capturedTextColor =
            new Color(1f, 0.48f, 0.68f, 0.88f);
        public Color capturedParticleColor =
            new Color(1f, 0.12f, 0.52f, 0.96f);

        [Header("任务方胜利配色")]
        public Color disguiserVictoryTextColor =
            new Color(0.42f, 1f, 0.64f, 0.9f);
        public Color disguiserVictoryParticleColor =
            new Color(0.12f, 1f, 0.72f, 0.96f);

        [Header("军官方胜利配色")]
        public Color officerVictoryTextColor =
            new Color(1f, 0.32f, 0.44f, 0.9f);
        public Color officerVictoryParticleColor =
            new Color(1f, 0.12f, 0.26f, 0.96f);
    }

    [ExecuteAlways]
    public sealed class GameEventPresentation : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset displayFont;
        [SerializeField] private GameEventPresentationSettings style =
            new GameEventPresentationSettings();
        [SerializeField] private string previewText = "任务已完成50%！";
        [SerializeField] private bool showPreview = true;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private EventBurstGraphic burst;
        private RectTransform shakeTarget;
        private Vector2 shakeOrigin;
        private Coroutine eventRoutine;
        private int countdownValue = int.MinValue;
        private bool goShown;
        private GameEventPresentationSettings settings;

        public GameEventPresentationSettings Settings => style;

        public static GameEventPresentation Create(Transform parent,
            TMP_FontAsset font, RectTransform shakeTarget,
            GameEventPresentationSettings settings)
        {
            GameEventPresentation existing =
                parent.GetComponentInChildren<GameEventPresentation>(true);
            if (existing != null)
            {
                existing.displayFont = font;
                existing.style = settings ?? existing.style;
                existing.Initialize(shakeTarget);
                return existing;
            }
            GameObject root = new GameObject("GameEventPresentation",
                typeof(RectTransform), typeof(CanvasGroup),
                typeof(GameEventPresentation));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.transform.SetAsLastSibling();

            GameEventPresentation view =
                root.GetComponent<GameEventPresentation>();
            view.group = root.GetComponent<CanvasGroup>();
            view.group.blocksRaycasts = false;
            view.group.interactable = false;
            view.settings = settings ??
                new GameEventPresentationSettings();
            view.shakeTarget = shakeTarget;
            view.shakeOrigin = shakeTarget == null
                ? Vector2.zero : shakeTarget.anchoredPosition;

            GameObject particles = new GameObject("ParticleBurst",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(EventBurstGraphic));
            particles.transform.SetParent(root.transform, false);
            RectTransform particlesRect =
                particles.GetComponent<RectTransform>();
            particlesRect.anchorMin = Vector2.zero;
            particlesRect.anchorMax = Vector2.one;
            particlesRect.offsetMin = particlesRect.offsetMax = Vector2.zero;
            view.burst = particles.GetComponent<EventBurstGraphic>();
            view.burst.raycastTarget = false;

            GameObject textObject = new GameObject("ArtTitleTMP",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.025f, 0.29f);
            textRect.anchorMax = new Vector2(0.975f, 0.71f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            view.title = textObject.GetComponent<TextMeshProUGUI>();
            view.title.font = font != null ? font : TMP_Settings.defaultFontAsset;
            view.title.fontSize = view.settings.baseFontSize;
            view.title.fontStyle = FontStyles.Bold;
            view.title.alignment = TextAlignmentOptions.Center;
            view.title.enableAutoSizing = true;
            view.title.fontSizeMin = view.settings.minimumFontSize;
            view.title.fontSizeMax = view.settings.maximumFontSize;
            view.title.enableWordWrapping = false;
            view.title.overflowMode = TextOverflowModes.Ellipsis;
            view.title.raycastTarget = false;
            view.HideImmediate();
            return view;
        }

        public void Initialize(RectTransform target)
        {
            shakeTarget = target;
            shakeOrigin = target == null
                ? Vector2.zero : target.anchoredPosition;
            settings = style ?? new GameEventPresentationSettings();
            EnsureVisuals();
            ApplyStyle();
            if (Application.isPlaying) HideImmediate();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                ApplyStyle();
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                ApplyStyle();
#endif
        }

        private void ApplyStyle()
        {
            settings = style ?? new GameEventPresentationSettings();
            if (group == null || title == null) return;
            title.font = displayFont != null ? displayFont : title.font;
            title.fontSize = settings.baseFontSize;
            title.enableAutoSizing = true;
            title.fontSizeMin = Mathf.Min(settings.minimumFontSize, 42f);
            title.fontSizeMax = Mathf.Min(settings.maximumFontSize, 124f);
            title.characterSpacing = settings.characterSpacing;
            title.outlineColor = new Color32(20, 28, 30, 205);
            title.outlineWidth = Mathf.Clamp(
                settings.outlineSize / 34f, 0.08f, 0.24f);
            if (!Application.isPlaying)
            {
                group.alpha = showPreview ? 1f : 0f;
                title.text = previewText;
                title.color = settings.progressTextColor;
            }
        }

        private void EnsureVisuals()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            }
            if (burst == null)
            {
                Transform found = transform.Find("ParticleBurst");
                GameObject particles = found == null
                    ? new GameObject("ParticleBurst", typeof(RectTransform),
                        typeof(CanvasRenderer), typeof(EventBurstGraphic))
                    : found.gameObject;
                particles.transform.SetParent(transform, false);
                RectTransform rect = particles.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                burst = particles.GetComponent<EventBurstGraphic>();
                burst.raycastTarget = false;
            }
            if (title == null)
            {
                Transform legacy = transform.Find("ArtTitle");
                if (legacy != null) legacy.gameObject.SetActive(false);
                Transform found = transform.Find("ArtTitleTMP");
                GameObject textObject = found == null
                    ? new GameObject("ArtTitleTMP", typeof(RectTransform),
                        typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                    : found.gameObject;
                textObject.transform.SetParent(transform, false);
                RectTransform rect = textObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.025f, 0.29f);
                rect.anchorMax = new Vector2(0.975f, 0.71f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                title = textObject.GetComponent<TextMeshProUGUI>();
                title.alignment = TextAlignmentOptions.Center;
                title.fontStyle = FontStyles.Bold;
                title.enableAutoSizing = true;
                title.enableWordWrapping = false;
                title.overflowMode = TextOverflowModes.Ellipsis;
                title.raycastTarget = false;
            }
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public void UpdateCountdown(float remaining)
        {
            if (remaining > 0f)
            {
                int value = Mathf.Clamp(Mathf.CeilToInt(remaining), 1, 3);
                if (value != countdownValue)
                {
                    countdownValue = value;
                    ShowImmediate(value.ToString(),
                        new Color(1f, 0.78f, 0.22f, 0.88f));
                    burst.Play(new Color(1f, 0.43f, 0.12f, 0.9f), 0.72f);
                    StartCoroutine(PunchTitle());
                }
                return;
            }

            if (goShown) return;
            goShown = true;
            ShowImmediate(settings.goText,
                new Color(0.42f, 1f, 0.64f, 0.92f));
            burst.Play(new Color(0.12f, 1f, 0.76f, 0.95f), 1.05f);
            StartCoroutine(HideCountdown());
        }

        public void PrepareCountdown()
        {
            countdownValue = int.MinValue;
            goShown = false;
            if (eventRoutine != null)
            {
                StopCoroutine(eventRoutine);
                eventRoutine = null;
            }
            StopAllCoroutines();
            HideImmediate();
        }

        public void ShowEvent(string message, Color textColor,
            Color particleColor, float duration, bool shake)
        {
            if (eventRoutine != null) StopCoroutine(eventRoutine);
            eventRoutine = StartCoroutine(
                ShowEventRoutine(message, textColor, particleColor,
                    duration, shake));
        }

        private IEnumerator ShowEventRoutine(string message, Color textColor,
            Color particleColor, float duration, bool shake)
        {
            ShowImmediate(message, textColor);
            burst.Play(particleColor, duration);
            if (shake) StartCoroutine(ShakeTwice());
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float appear = Mathf.Clamp01(elapsed / 0.16f);
                float disappear = Mathf.Clamp01((duration - elapsed) / 0.48f);
                group.alpha = Mathf.Min(appear, disappear);
                float punch = 1f + Mathf.Sin(
                    Mathf.Clamp01(elapsed / 0.32f) * Mathf.PI) * 0.16f;
                punch = Mathf.Round(punch * 20f) / 20f;
                title.rectTransform.localScale =
                    new Vector3(punch, punch, 1f);
                yield return null;
            }
            HideImmediate();
            eventRoutine = null;
        }

        private IEnumerator PunchTitle()
        {
            float elapsed = 0f;
            while (elapsed < 0.34f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = 1f + Mathf.Sin(
                    Mathf.Clamp01(elapsed / 0.34f) * Mathf.PI) * 0.28f;
                scale = Mathf.Round(scale * 16f) / 16f;
                title.rectTransform.localScale =
                    new Vector3(scale, scale, 1f);
                yield return null;
            }
            title.rectTransform.localScale = Vector3.one;
        }

        private IEnumerator HideCountdown()
        {
            yield return new WaitForSecondsRealtime(settings.goDuration);
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / 0.3f);
                yield return null;
            }
            HideImmediate();
        }

        private IEnumerator ShakeTwice()
        {
            if (shakeTarget == null) yield break;
            const float duration = 0.52f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(elapsed / duration * Mathf.PI * 4f);
                float cross = Mathf.Sin(elapsed / duration * Mathf.PI * 8f);
                shakeTarget.anchoredPosition = shakeOrigin +
                    new Vector2(wave * 15f, cross * 6f) * fade;
                yield return null;
            }
            shakeTarget.anchoredPosition = shakeOrigin;
        }

        private void ShowImmediate(string message, Color color)
        {
            gameObject.SetActive(true);
            group.alpha = 1f;
            title.text = message;
            title.color = color;
            title.rectTransform.localScale = Vector3.one;
            transform.SetAsLastSibling();
        }

        private void HideImmediate()
        {
            if (group != null) group.alpha = 0f;
            if (title != null)
            {
                title.text = "";
                title.rectTransform.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            if (shakeTarget != null)
                shakeTarget.anchoredPosition = shakeOrigin;
        }
    }

    [ExecuteAlways]
    public sealed class PixelCharacterSpacing : BaseMeshEffect
    {
        [SerializeField, Range(-12f, 32f)] private float spacing = 2f;

        public float Spacing
        {
            get { return spacing; }
            set
            {
                if (Mathf.Approximately(spacing, value)) return;
                spacing = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || Mathf.Approximately(spacing, 0f)) return;
            List<UIVertex> vertices = new List<UIVertex>();
            helper.GetUIVertexStream(vertices);
            int characterCount = vertices.Count / 6;
            if (characterCount <= 1) return;

            float center = (characterCount - 1) * 0.5f;
            for (int character = 0; character < characterCount; character++)
            {
                float offset = (character - center) * spacing;
                int start = character * 6;
                for (int vertexIndex = start;
                     vertexIndex < start + 6 &&
                     vertexIndex < vertices.Count; vertexIndex++)
                {
                    UIVertex vertex = vertices[vertexIndex];
                    Vector3 position = vertex.position;
                    position.x += Mathf.Round(offset);
                    vertex.position = position;
                    vertices[vertexIndex] = vertex;
                }
            }
            helper.Clear();
            helper.AddUIVertexTriangleStream(vertices);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }

    public sealed class EventBurstGraphic : MaskableGraphic
    {
        private struct Spark
        {
            public float angle;
            public float radius;
            public float speed;
            public float size;
            public float phase;
        }

        private readonly List<Spark> sparks = new List<Spark>();
        private float startedAt = -100f;
        private float duration = 1f;
        private Color burstColor = Color.white;

        protected override void Awake()
        {
            base.Awake();
            for (int index = 0; index < 128; index++)
            {
                sparks.Add(new Spark
                {
                    angle = index * 137.508f * Mathf.Deg2Rad,
                    radius = 20f + (index % 12) * 10f,
                    speed = 210f + (index % 17) * 20f,
                    size = 5f + (index % 6) * 2.8f,
                    phase = (index % 17) / 17f
                });
            }
        }

        public void Play(Color color, float lifetime)
        {
            burstColor = color;
            duration = Mathf.Max(0.4f, lifetime);
            startedAt = Time.unscaledTime;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (Time.unscaledTime - startedAt <= duration)
                SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float age = Time.unscaledTime - startedAt;
            if (age < 0f || age > duration) return;
            float progress = Mathf.Clamp01(age / duration);
            float steppedProgress = Mathf.Floor(progress * 24f) / 24f;
            float alpha = Mathf.Floor(
                Mathf.Sin(progress * Mathf.PI) * 5f) / 5f;
            Vector2 center = rectTransform.rect.center;
            for (int index = 0; index < sparks.Count; index++)
            {
                Spark spark = sparks[index];
                float local = Mathf.Repeat(
                    steppedProgress + spark.phase * 0.18f, 1f);
                float snappedAngle = Mathf.Round(
                    spark.angle / (Mathf.PI * 0.25f)) *
                    (Mathf.PI * 0.25f);
                Vector2 direction =
                    new Vector2(Mathf.Cos(snappedAngle),
                        Mathf.Sin(snappedAngle));
                Vector2 position = center + direction *
                    (spark.radius + local * spark.speed);
                position.x = Mathf.Round(position.x / 10f) * 10f;
                position.y = Mathf.Round(position.y / 10f) * 10f;
                float size = Mathf.Max(8f,
                    Mathf.Round(spark.size / 4f) * 4f);
                Color color = burstColor;
                color = QuantizeColor(color, index);
                color.a *= Mathf.Floor(
                    Mathf.Min(1f, alpha * 1.35f) *
                    (1f - local * 0.52f) * 4f) / 4f;
                AddPixelCluster(vh, position, direction, size, color, index);
            }

            AddPixelRing(vh, center, steppedProgress, burstColor);
        }

        private static Color QuantizeColor(Color source, int index)
        {
            if (index % 7 == 0)
                return new Color(1f, 0.95f, 0.68f, source.a);
            if (index % 5 == 0)
                return Color.Lerp(source, Color.white, 0.42f);
            return new Color(
                Mathf.Round(source.r * 4f) / 4f,
                Mathf.Round(source.g * 4f) / 4f,
                Mathf.Round(source.b * 4f) / 4f,
                source.a);
        }

        private static void AddPixelCluster(
            VertexHelper vh, Vector2 position, Vector2 direction,
            float size, Color color, int index)
        {
            AddPixel(vh, position, new Vector2(size, size), color);

            Vector2 trailStep = direction * -size;
            Color trailColor = color;
            trailColor.a *= 0.72f;
            AddPixel(vh, position + trailStep,
                new Vector2(size * 0.72f, size * 0.72f), trailColor);

            if (index % 3 == 0)
            {
                Vector2 side = new Vector2(-direction.y, direction.x) * size;
                AddPixel(vh, position + side,
                    new Vector2(size * 0.58f, size * 0.58f), trailColor);
            }
            if (index % 8 == 0)
                AddPixelCross(vh, position, size * 0.9f, color);
        }

        private static void AddPixelRing(
            VertexHelper vh, Vector2 center, float progress, Color color)
        {
            const int blockCount = 36;
            float pulse = Mathf.Sin(Mathf.Clamp01(progress * 1.45f) *
                                    Mathf.PI);
            float radius = Mathf.Lerp(55f, 360f, progress);
            color.a *= pulse * 0.82f;
            for (int index = 0; index < blockCount; index++)
            {
                float angle = index * Mathf.PI * 2f / blockCount;
                Vector2 position = center + new Vector2(
                    Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                position.x = Mathf.Round(position.x / 6f) * 6f;
                position.y = Mathf.Round(position.y / 6f) * 6f;
                float size = index % 3 == 0 ? 14f : 8f;
                AddPixel(vh, position, new Vector2(size, size), color);
            }
        }

        private static void AddPixelCross(
            VertexHelper vh, Vector2 center, float size, Color color)
        {
            float unit = Mathf.Max(4f, Mathf.Round(size / 4f) * 4f);
            AddPixel(vh, center, new Vector2(unit * 3f, unit), color);
            AddPixel(vh, center, new Vector2(unit, unit * 3f), color);
        }

        private static void AddPixel(
            VertexHelper vh, Vector2 center, Vector2 size, Color color)
        {
            int start = vh.currentVertCount;
            Vector2 half = size * 0.5f;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center + new Vector2(-half.x, -half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(-half.x, half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(half.x, half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(half.x, -half.y);
            vh.AddVert(vertex);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddQuad(VertexHelper vh, Vector2 center,
            Vector2 direction, Vector2 tangent, float length, float width,
            Color color)
        {
            int start = vh.currentVertCount;
            Vector2 forward = direction * length * 0.5f;
            Vector2 side = tangent * width * 0.5f;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center - forward - side;
            vh.AddVert(vertex);
            vertex.position = center - forward + side;
            vh.AddVert(vertex);
            vertex.position = center + forward + side;
            vh.AddVert(vertex);
            vertex.position = center + forward - side;
            vh.AddVert(vertex);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
