using FrogCamp.Networking;
using UnityEngine;

namespace FrogCamp.Gameplay
{
    public sealed class FrogActorView : MonoBehaviour
    {
        private RectTransform rect;
        private FrogGraphic graphic;
        private Vector2 targetPosition;
        private int actionId = -1;
        private float localActionStart;
        private GameActorData data;

        public string ActorId { get; private set; }
        public float SortY { get { return data == null ? 0f : data.y; } }

        public static FrogActorView Create(RectTransform parent, GameActorData actor)
        {
            GameObject instance = new GameObject("Frog_" + actor.id,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(FrogGraphic),
                typeof(FrogActorView));
            instance.transform.SetParent(parent, false);
            FrogActorView view = instance.GetComponent<FrogActorView>();
            view.rect = instance.GetComponent<RectTransform>();
            view.rect.sizeDelta = new Vector2(76f, 96f);
            view.graphic = instance.GetComponent<FrogGraphic>();
            view.graphic.raycastTarget = false;
            view.ActorId = actor.id;
            view.Apply(actor, true);
            return view;
        }

        public void Apply(GameActorData actor, bool immediate = false)
        {
            data = actor;
            gameObject.SetActive(!actor.eliminated && actor.online);
            targetPosition = new Vector2(actor.x * 2f - 960f, 540f - actor.y * 2f);
            if (immediate) rect.anchoredPosition = targetPosition;
            if (actor.actionId != actionId)
            {
                actionId = actor.actionId;
                localActionStart = Time.unscaledTime;
            }
            string[] facings =
            {
                "up", "upRight", "right", "downRight",
                "down", "downLeft", "left", "upLeft"
            };
            int facingIndex = System.Array.IndexOf(facings, actor.facing);
            rect.localRotation = Quaternion.Euler(0f, 0f, -(facingIndex < 0 ? 0 : facingIndex * 45f));
        }

        private void Update()
        {
            if (data == null || data.eliminated) return;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            float duration = GameSimulation.ActionDuration(data.action);
            float progress = duration > 0f
                ? Mathf.Clamp01((Time.unscaledTime - localActionStart) / duration) : 0f;
            float hop = data.moving ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7.5f)) : 0f;
            float jump = data.action == "jump" ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float idle = !data.moving && string.IsNullOrEmpty(data.action)
                ? Mathf.Sin(Time.unscaledTime * 4.5f) : 0f;
            rect.localScale = new Vector3(1f + idle * .018f,
                1f - idle * .025f, 1f);
            rect.anchoredPosition += Vector2.up * (hop * 6f + jump * 24f);
            graphic.SetPose(data.role, data.action, progress, data.moving,
                data.stunned, Time.unscaledTime);
        }
    }
}
