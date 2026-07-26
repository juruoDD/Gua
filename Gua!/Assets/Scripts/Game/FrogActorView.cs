using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.Gameplay
{
    public sealed class FrogActorView : MonoBehaviour
    {
        private RectTransform rect;
        private FrogGraphic graphic;
        private RawImage frameImage;
        private FrogAnimationSet animations;
        private Vector2 targetPosition;
        private int actionId = -1;
        private float localActionStart;
        private GameActorData data;

        public string ActorId { get; private set; }
        public float SortY { get { return data == null ? 0f : data.y; } }

        public static FrogActorView Create(RectTransform parent, GameActorData actor,
            FrogAnimationSet greenAnimations, FrogAnimationSet pinkAnimations)
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
            view.animations = actor.role == "officer" ? pinkAnimations : greenAnimations;
            if (view.animations != null)
            {
                GameObject frameObject = new GameObject("FrogFrameAnimation",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                frameObject.transform.SetParent(instance.transform, false);
                RectTransform frameRect = frameObject.GetComponent<RectTransform>();
                frameRect.anchorMin = frameRect.anchorMax = new Vector2(.5f, .5f);
                frameRect.sizeDelta = new Vector2(82f, 82f);
                view.frameImage = frameObject.GetComponent<RawImage>();
                view.frameImage.raycastTarget = false;
            }
            view.ActorId = actor.id;
            view.Apply(actor, true);
            return view;
        }

        public void Apply(GameActorData actor, bool immediate = false)
        {
            data = actor;
            gameObject.SetActive(actor.online);
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
            string shownFacing = !string.IsNullOrEmpty(actor.action) &&
                                 !string.IsNullOrEmpty(actor.actionFacing)
                ? actor.actionFacing : actor.facing;
            int facingIndex = System.Array.IndexOf(facings, shownFacing);
            rect.localRotation = Quaternion.Euler(0f, 0f, -(facingIndex < 0 ? 0 : facingIndex * 45f));
        }

        private void Update()
        {
            if (data == null) return;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            float duration = GameSimulation.ActionDuration(data.action);
            float progress = duration > 0f
                ? Mathf.Clamp01((Time.unscaledTime - localActionStart) / duration) : 0f;
            string specialState = data.eliminated ? "death"
                : data.stunned ? "stun" : data.action;
            Texture2D actionTexture = animations == null
                ? null : animations.GetActionTexture(specialState);
            bool canUseFrames = frameImage != null && animations != null;
            bool useActionFrames = canUseFrames && actionTexture != null;
            bool useHopFrames = !data.eliminated && !data.stunned &&
                                canUseFrames && animations.Hop != null &&
                                ((string.IsNullOrEmpty(data.action) && data.moving) ||
                                 GameSimulation.IsCadenceMoveAction(data.action));
            bool useIdleFrames = !data.eliminated && !data.stunned &&
                                 canUseFrames && animations.Idle != null &&
                                 string.IsNullOrEmpty(data.action) && !data.moving;
            float hop = data.moving && !useHopFrames
                ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7.5f)) : 0f;
            float jump = data.action == "jump" && !useActionFrames
                ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float idle = !data.eliminated && !data.stunned &&
                         !useIdleFrames && !data.moving && string.IsNullOrEmpty(data.action)
                ? Mathf.Sin(Time.unscaledTime * 4.5f) : 0f;
            rect.localScale = new Vector3(1f + idle * .018f,
                1f - idle * .025f, 1f);
            rect.anchoredPosition += Vector2.up * (hop * 3f + jump * 12f);
            bool useExternalFrames = useIdleFrames || useHopFrames || useActionFrames;
            graphic.enabled = !useExternalFrames;
            if (frameImage != null)
            {
                frameImage.enabled = useExternalFrames;
                if (useExternalFrames)
                {
                    string state = useActionFrames ? specialState :
                        (useHopFrames ? "hop" : "idle");
                    int frameCount = FrogAnimationSet.GetFrameCount(state);
                    int frame;
                    if (state == "death")
                    {
                        frame = Mathf.Min(frameCount - 1,
                            Mathf.FloorToInt(progress * frameCount));
                    }
                    else if (state == "stun" || !useActionFrames)
                    {
                        frame = Mathf.FloorToInt(Time.unscaledTime * 8f *
                            GameSimulation.AnimationSpeedMultiplier) % frameCount;
                    }
                    else
                    {
                        frame = Mathf.Min(frameCount - 1,
                            Mathf.FloorToInt(progress * frameCount));
                    }
                    frameImage.texture = useActionFrames ? actionTexture :
                        (useHopFrames ? animations.Hop : animations.Idle);
                    frameImage.uvRect = new Rect(frame / (float)frameCount, 0f,
                        1f / frameCount, 1f);
                    RectTransform frameRect = (RectTransform)frameImage.transform;
                    bool tallFrame = useHopFrames || useActionFrames;
                    frameRect.sizeDelta = tallFrame
                        ? new Vector2(82f, 164f) : new Vector2(82f, 82f);
                    frameRect.anchoredPosition =
                        FrogAnimationSet.GetFrameOffset(state, frame);
                    float alpha = state == "death"
                        ? Mathf.Lerp(1f, 0.48f, Mathf.SmoothStep(0f, 1f, progress))
                        : 1f;
                    frameImage.color = new Color(1f, 1f, 1f, alpha);
                }
            }
            graphic.SetPose(data.role, data.action, progress, data.moving,
                data.stunned, Time.unscaledTime);
        }
    }
}
