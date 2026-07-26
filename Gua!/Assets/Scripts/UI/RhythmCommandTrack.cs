using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class RhythmCommandTrack : MonoBehaviour
    {
        [SerializeField] private RectTransform targetMarker;
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform[] noteSlots;
        [SerializeField] private Text[] noteLabels;
        [SerializeField] private Image[] noteImages;
        [SerializeField] private Sprite[] commandSprites = new Sprite[10];
        [SerializeField] private TaikoHitEffectGraphic hitEffect;
        [SerializeField] private RectTransform judgementRoot;
        [SerializeField] private Text judgementText;
        [SerializeField, Range(1.5f, 5f)] private float leadTime = 3f;
        [SerializeField, Range(0.1f, 0.5f)] private float passWindow = 0.24f;
        [SerializeField, Range(0.5f, 1f)]
        private float noteVisualScale = 0.78f;
        [SerializeField, Range(0.2f, 0.8f)]
        private float feedbackDuration = 0.46f;

        private static readonly string[] Commands =
        {
            "armLeft", "armRight", "legLeft", "legRight",
            "moveUp", "moveDown", "moveLeft", "moveRight",
            "salute", "croak"
        };

        private static readonly string[] Labels =
        {
            "左手", "右手", "左脚", "右脚",
            "上", "下", "左", "右", "敬礼", "呱"
        };

        private Sprite[] fallbackSprites;
        private Vector3[] slotBaseScales;
        private Vector3 targetBaseScale = Vector3.one;
        private Color targetBaseColor = Color.white;
        private Vector2 judgementBasePosition;
        private Vector3 judgementBaseScale = Vector3.one;
        private float approachPulse;
        private float hitTimer;
        private float feedbackTimer;
        private int lastCadenceBeat = -1;
        private int lastDanceBeat = -1;
        private string lastSpecialPhase;

        public void Configure(RectTransform marker, Image markerImage,
            RectTransform[] slots, Text[] labels, Image[] images)
        {
            targetMarker = marker;
            targetImage = markerImage;
            noteSlots = slots;
            noteLabels = labels;
            noteImages = images;
            CacheVisualDefaults();
        }

        public void ConfigureTaiko(Sprite[] sprites,
            TaikoHitEffectGraphic effect, RectTransform feedbackRoot,
            Text feedbackText)
        {
            commandSprites = sprites;
            hitEffect = effect;
            judgementRoot = feedbackRoot;
            judgementText = feedbackText;
            CacheVisualDefaults();
        }

        private void Awake()
        {
            CacheVisualDefaults();
            SetAllNotesActive(false);
            if (judgementRoot != null)
                judgementRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            AnimateTarget();
            AnimateFeedback();
        }

        public void Apply(GameStateData game)
        {
            if (game == null || noteSlots == null) return;
            if (GameSimulation.IsDanceSequenceActive(game))
            {
                ApplyDanceSequence(game);
                return;
            }

            DetectCadenceHit(game);
            lastSpecialPhase = null;
            lastDanceBeat = -1;

            var beats = CadenceBeatTable.Points;
            GetTrackCoordinates(
                out float targetX, out float spawnX, out float laneY);
            int commandIndex =
                Mathf.Clamp(game.nextCadenceBeat, 0, beats.Count);
            float cycleOffset = 0f;
            float loopLength = CadenceBeatTable.LoopEndTime -
                               CadenceBeatTable.LoopStartTime;
            bool targetPulse = false;

            for (int slot = 0; slot < noteSlots.Length; slot++)
            {
                if (commandIndex >= beats.Count)
                {
                    commandIndex = CadenceBeatTable.LoopStartIndex;
                    cycleOffset += loopLength;
                }
                if (commandIndex < 0 || commandIndex >= beats.Count ||
                    commandIndex >= game.cadenceCommands.Count)
                {
                    SetNoteActive(slot, false);
                    continue;
                }

                float remaining = beats[commandIndex].time + cycleOffset -
                                  game.musicTime;
                if (remaining > leadTime || remaining < -passWindow)
                {
                    SetNoteActive(slot, false);
                    commandIndex++;
                    continue;
                }

                string command = game.cadenceCommands[commandIndex];
                int kind = CommandIndex(command);
                float travel = Mathf.Clamp01(remaining / leadTime);
                float x = Mathf.Lerp(targetX, spawnX, travel);
                float hit = HitStrength(remaining);
                targetPulse |= hit > 0f;
                ApplyNote(slot, kind, x, laneY, remaining, hit);
                commandIndex++;
            }

            SetTargetPulse(targetPulse);
        }

        private void ApplyDanceSequence(GameStateData game)
        {
            DetectDanceHit(game);
            if (game.specialMusicPhase == GameSimulation.DancePhaseWhistle ||
                game.specialMusicPhase == GameSimulation.DancePhasePause)
            {
                SetAllNotesActive(false);
                SetTargetPulse(false);
                return;
            }

            float danceTime =
                game.specialMusicPhase == GameSimulation.DancePhaseBell
                    ? game.specialMusicTime -
                      GameSimulation.BellSoundDuration
                    : game.specialMusicTime;
            int commandIndex = Mathf.Max(0, game.nextDanceBeat - 1);
            GetTrackCoordinates(
                out float targetX, out float spawnX, out float laneY);
            bool targetPulse = false;

            for (int slot = 0; slot < noteSlots.Length; slot++)
            {
                if (commandIndex < 0 ||
                    commandIndex >= GameSimulation.DanceActionCount ||
                    commandIndex >= game.danceCommands.Count)
                {
                    SetNoteActive(slot, false);
                    continue;
                }

                float beatTime = GameSimulation.DanceActionStartTime +
                    commandIndex * GameSimulation.DanceActionInterval;
                float remaining = beatTime - danceTime;
                if (remaining > leadTime || remaining < -passWindow)
                {
                    SetNoteActive(slot, false);
                    commandIndex++;
                    continue;
                }

                int kind = CommandIndex(
                    game.danceCommands[commandIndex]);
                float travel = Mathf.Clamp01(remaining / leadTime);
                float hit = HitStrength(remaining);
                targetPulse |= hit > 0f;
                ApplyNote(slot, kind,
                    Mathf.Lerp(targetX, spawnX, travel),
                    laneY, remaining, hit);
                commandIndex++;
            }

            SetTargetPulse(targetPulse);
        }

        private void ApplyNote(int slot, int kind, float x, float y,
            float remaining, float hit)
        {
            if (!IsValidSlot(slot)) return;
            RectTransform root = noteSlots[slot];
            root.gameObject.SetActive(true);
            root.anchoredPosition = new Vector2(x, y);

            float entrance = Mathf.Clamp01(
                (leadTime - remaining) / 0.16f);
            float passedFade = remaining < 0f
                ? 1f - Mathf.Clamp01(-remaining / passWindow)
                : 1f;
            float scale = Mathf.Lerp(0.88f, 1f, entrance) +
                          hit * 0.17f;
            Vector3 baseScale =
                slotBaseScales != null && slot < slotBaseScales.Length
                    ? slotBaseScales[slot]
                    : Vector3.one;
            root.localScale = baseScale * (scale * noteVisualScale);

            Sprite commandSprite =
                commandSprites != null && kind < commandSprites.Length
                    ? commandSprites[kind]
                    : null;
            if (noteImages != null && slot < noteImages.Length &&
                noteImages[slot] != null)
            {
                Image image = noteImages[slot];
                image.sprite = commandSprite != null
                    ? commandSprite
                    : FallbackSprite(slot);
                image.preserveAspect = commandSprite != null;
                Color color = commandSprite != null
                    ? Color.white
                    : CommandColor(kind);
                color = Color.Lerp(
                    color, CampUiFactory.Hex("#FFD85A"), hit * 0.38f);
                color.a *= passedFade;
                image.color = color;
            }

            if (noteLabels != null && slot < noteLabels.Length &&
                noteLabels[slot] != null)
            {
                Text label = noteLabels[slot];
                label.text = Labels[kind];
                Color labelColor = Color.Lerp(
                    CampUiFactory.White,
                    CampUiFactory.Hex("#FFF19B"), hit);
                labelColor.a *= passedFade;
                label.color = labelColor;
            }
        }

        private void DetectCadenceHit(GameStateData game)
        {
            int current = game.nextCadenceBeat;
            if (lastCadenceBeat >= 0 && current != lastCadenceBeat)
                TriggerHitFeedback();
            lastCadenceBeat = current;
        }

        private void DetectDanceHit(GameStateData game)
        {
            if (lastSpecialPhase != game.specialMusicPhase)
            {
                lastSpecialPhase = game.specialMusicPhase;
                lastDanceBeat = game.nextDanceBeat;
                return;
            }
            if (game.specialMusicPhase != GameSimulation.DancePhaseMusic)
                return;
            if (lastDanceBeat >= 0 &&
                game.nextDanceBeat != lastDanceBeat)
                TriggerHitFeedback();
            lastDanceBeat = game.nextDanceBeat;
        }

        private void TriggerHitFeedback()
        {
            hitTimer = 1f;
            feedbackTimer = feedbackDuration;
            if (hitEffect != null) hitEffect.Trigger();
            if (judgementText != null)
            {
                judgementText.text = "好！";
                Color color = judgementText.color;
                color.a = 1f;
                judgementText.color = color;
            }
            if (judgementRoot != null)
                judgementRoot.gameObject.SetActive(true);
        }

        private void AnimateTarget()
        {
            if (targetMarker == null) return;
            hitTimer = Mathf.MoveTowards(
                hitTimer, 0f, Time.unscaledDeltaTime * 4.2f);
            float burst = Mathf.Sin(
                Mathf.Clamp01(hitTimer) * Mathf.PI);
            float scale = 1f + approachPulse * 0.09f +
                          burst * 0.22f;
            targetMarker.localScale = targetBaseScale * scale;
            if (targetImage == null) return;
            float glow = Mathf.Max(
                approachPulse * 0.45f, burst);
            targetImage.color = Color.Lerp(
                targetBaseColor,
                CampUiFactory.Hex("#FFD85A"), glow);
        }

        private void AnimateFeedback()
        {
            if (judgementRoot == null || feedbackTimer <= 0f) return;
            feedbackTimer = Mathf.Max(
                0f, feedbackTimer - Time.unscaledDeltaTime);
            float progress = 1f - feedbackTimer / feedbackDuration;
            float pop = progress < 0.2f
                ? Mathf.Lerp(0.55f, 1.18f, progress / 0.2f)
                : Mathf.Lerp(1.18f, 0.94f,
                    (progress - 0.2f) / 0.8f);
            judgementRoot.localScale = judgementBaseScale * pop;
            judgementRoot.anchoredPosition =
                judgementBasePosition + Vector2.up *
                Mathf.Lerp(0f, 24f, progress);
            if (judgementText != null)
            {
                Color color = judgementText.color;
                color.a = 1f - Mathf.Clamp01(
                    (progress - 0.62f) / 0.38f);
                judgementText.color = color;
            }
            if (feedbackTimer > 0f) return;
            judgementRoot.gameObject.SetActive(false);
            judgementRoot.localScale = judgementBaseScale;
            judgementRoot.anchoredPosition =
                judgementBasePosition;
        }

        private void SetTargetPulse(bool pulse)
        {
            approachPulse = pulse ? 1f : 0f;
        }

        public void ApplyEditorPreview()
        {
            if (noteSlots == null) return;
            GetTrackCoordinates(
                out float targetX, out float spawnX, out float laneY);
            for (int index = 0; index < noteSlots.Length; index++)
            {
                SetNoteActive(index, index < 5);
                if (index >= 5 || !IsValidSlot(index)) continue;
                noteSlots[index].anchoredPosition = new Vector2(
                    Mathf.Lerp(targetX + 130f, spawnX,
                        index / 4f), laneY);
                ApplyNote(index, index,
                    noteSlots[index].anchoredPosition.x,
                    laneY, leadTime * index / 5f, 0f);
            }
            SetTargetPulse(false);
        }

        private void CacheVisualDefaults()
        {
            if (targetMarker != null)
                targetBaseScale = targetMarker.localScale;
            if (targetImage != null)
                targetBaseColor = targetImage.color;
            if (judgementRoot != null)
            {
                judgementBasePosition =
                    judgementRoot.anchoredPosition;
                judgementBaseScale = judgementRoot.localScale;
            }
            if (noteSlots != null)
            {
                slotBaseScales = new Vector3[noteSlots.Length];
                for (int index = 0; index < noteSlots.Length; index++)
                    slotBaseScales[index] = noteSlots[index] != null
                        ? noteSlots[index].localScale
                        : Vector3.one;
            }
            if (noteImages != null)
            {
                fallbackSprites = new Sprite[noteImages.Length];
                for (int index = 0; index < noteImages.Length; index++)
                    fallbackSprites[index] = noteImages[index] != null
                        ? noteImages[index].sprite
                        : null;
            }
        }

        private void GetTrackCoordinates(out float targetX,
            out float spawnX, out float laneY)
        {
            RectTransform track = (RectTransform)transform;
            Rect area = track.rect;
            Vector3 targetLocal = targetMarker != null
                ? track.InverseTransformPoint(targetMarker.position)
                : new Vector3(
                    area.xMin + area.width * 0.085f, 0f, 0f);
            targetX = targetLocal.x;
            laneY = targetLocal.y;
            spawnX = area.xMax - 52f;
        }

        private float HitStrength(float remaining)
        {
            return 1f - Mathf.Clamp01(
                Mathf.Abs(remaining) / 0.14f);
        }

        private int CommandIndex(string command)
        {
            int kind = System.Array.IndexOf(Commands, command);
            return kind < 0 ? 0 : kind;
        }

        private bool IsValidSlot(int slot)
        {
            return noteSlots != null && slot >= 0 &&
                   slot < noteSlots.Length &&
                   noteSlots[slot] != null;
        }

        private void SetNoteActive(int slot, bool active)
        {
            if (IsValidSlot(slot))
                noteSlots[slot].gameObject.SetActive(active);
        }

        private void SetAllNotesActive(bool active)
        {
            if (noteSlots == null) return;
            for (int slot = 0; slot < noteSlots.Length; slot++)
                SetNoteActive(slot, active);
        }

        private Sprite FallbackSprite(int slot)
        {
            return fallbackSprites != null &&
                   slot >= 0 && slot < fallbackSprites.Length
                ? fallbackSprites[slot]
                : null;
        }

        private static Color CommandColor(int kind)
        {
            if (kind < 2) return CampUiFactory.Hex("#F06B4F");
            if (kind < 4) return CampUiFactory.Hex("#66B9C5");
            if (kind < 8) return CampUiFactory.Hex("#82B85B");
            return CampUiFactory.Hex("#E5B84F");
        }
    }
}
