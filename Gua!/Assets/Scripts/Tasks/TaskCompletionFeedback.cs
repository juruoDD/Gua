using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.Tasks
{
    /// <summary>
    /// Keeps a visual copy of a completed task alive long enough to give
    /// completion feedback before it leaves the task panel.
    /// </summary>
    internal sealed class TaskCompletionFeedback : MonoBehaviour
    {
        private const float ShakeDuration = 0.42f;
        private const float ExitDuration = 0.48f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 restingPosition;
        private Vector3 restingScale;
        private Quaternion restingRotation;

        public static void Play(Transform sourceRow)
        {
            if (sourceRow == null || !sourceRow.gameObject.activeInHierarchy)
                return;

            Canvas canvas = sourceRow.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            GameObject snapshot = Instantiate(
                sourceRow.gameObject, canvas.transform, true);
            snapshot.name = "TaskCompletionFeedback";
            snapshot.transform.SetAsLastSibling();

            foreach (Graphic graphic in snapshot.GetComponentsInChildren<Graphic>())
                graphic.raycastTarget = false;

            RectTransform snapshotRect = snapshot.GetComponent<RectTransform>();
            AddParticles(snapshotRect);
            TaskCompletionPanelShake.Play(FindTaskPanel(sourceRow));

            TaskCompletionFeedback feedback =
                snapshot.AddComponent<TaskCompletionFeedback>();
            feedback.Begin();
        }

        private static Transform FindTaskPanel(Transform sourceRow)
        {
            Transform current = sourceRow;
            while (current != null)
            {
                if (current.name == "TaskPanel") return current;
                current = current.parent;
            }
            return sourceRow.parent;
        }

        private static void AddParticles(RectTransform parent)
        {
            GameObject particleObject = new GameObject(
                "CompletionParticles", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TaskCompletionParticles));
            RectTransform particles = particleObject.GetComponent<RectTransform>();
            particles.SetParent(parent, false);
            particles.anchorMin = Vector2.zero;
            particles.anchorMax = Vector2.one;
            particles.offsetMin = new Vector2(-150f, -110f);
            particles.offsetMax = new Vector2(150f, 110f);
            particles.SetAsLastSibling();
            particleObject.GetComponent<TaskCompletionParticles>().Play();
        }

        private void Begin()
        {
            rectTransform = (RectTransform)transform;
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            restingPosition = rectTransform.localPosition;
            restingScale = rectTransform.localScale;
            restingRotation = rectTransform.localRotation;
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            while (elapsed < ShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / ShakeDuration);
                float strength = Mathf.Lerp(18f, 2f, progress);
                float punch = 1f + Mathf.Sin(progress * Mathf.PI) * 0.14f;
                rectTransform.localPosition = restingPosition +
                    new Vector3(
                        Mathf.Sin(elapsed * 78f) * strength,
                        Mathf.Cos(elapsed * 61f) * strength * 0.45f, 0f);
                rectTransform.localRotation = restingRotation *
                    Quaternion.Euler(0f, 0f,
                        Mathf.Sin(elapsed * 55f) * strength * 0.22f);
                rectTransform.localScale = restingScale * punch;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < ExitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / ExitDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                rectTransform.localPosition = restingPosition +
                    new Vector3(0f, 48f * eased, 0f);
                rectTransform.localRotation = restingRotation;
                rectTransform.localScale = restingScale *
                    Mathf.Lerp(1f, 0.82f, eased);
                canvasGroup.alpha = 1f - progress * progress;
                yield return null;
            }

            Destroy(gameObject);
        }
    }

    internal sealed class TaskCompletionParticles : MaskableGraphic
    {
        private const int ParticleCount = 36;
        private float startedAt;
        private bool playing;

        public void Play()
        {
            raycastTarget = false;
            startedAt = Time.unscaledTime;
            playing = true;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!playing) return;
            SetVerticesDirty();
            if (Time.unscaledTime - startedAt > 0.9f)
            {
                playing = false;
                canvasRenderer.Clear();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!playing) return;

            float time = Time.unscaledTime - startedAt;
            float life = Mathf.Clamp01(time / 0.8f);
            for (int index = 0; index < ParticleCount; index++)
            {
                float angle = index * Mathf.PI * 2f / ParticleCount +
                              Hash(index * 19) * 0.28f;
                float speed = Mathf.Lerp(105f, 260f, Hash(index * 37 + 5));
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 position = direction * speed * time;
                position.y -= 78f * time * time;
                float size = Mathf.Lerp(7f, 16f, Hash(index * 13 + 2)) *
                             (1f - life * 0.45f);
                Color particleColor = index % 4 == 0
                    ? new Color(1f, 0.9f, 0.28f, 1f - life)
                    : new Color(0.42f, 1f, 0.48f, 1f - life);
                AddQuad(vh, position, size, particleColor);
            }
        }

        private static void AddQuad(
            VertexHelper vh, Vector2 center, float size, Color color)
        {
            int start = vh.currentVertCount;
            Vector2 half = Vector2.one * size * 0.5f;
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

        private static float Hash(int value)
        {
            return Mathf.Abs(Mathf.Sin(value * 12.9898f) * 43758.5453f) % 1f;
        }
    }

    internal sealed class TaskCompletionPanelShake : MonoBehaviour
    {
        private Coroutine shakeRoutine;
        private Vector3 restingPosition;

        public static void Play(Transform panel)
        {
            if (panel == null) return;
            TaskCompletionPanelShake shake =
                panel.GetComponent<TaskCompletionPanelShake>();
            if (shake == null)
                shake = panel.gameObject.AddComponent<TaskCompletionPanelShake>();
            shake.Restart();
        }

        private void Restart()
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                transform.localPosition = restingPosition;
            }
            restingPosition = transform.localPosition;
            shakeRoutine = StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            const float duration = 0.36f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float strength = Mathf.Lerp(10f, 0f, progress);
                transform.localPosition = restingPosition + new Vector3(
                    Mathf.Sin(elapsed * 72f) * strength,
                    Mathf.Cos(elapsed * 57f) * strength * 0.42f, 0f);
                yield return null;
            }

            transform.localPosition = restingPosition;
            shakeRoutine = null;
        }

        private void OnDisable()
        {
            if (shakeRoutine == null) return;
            transform.localPosition = restingPosition;
            shakeRoutine = null;
        }
    }
}
