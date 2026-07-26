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
        [SerializeField, Range(1.5f, 5f)] private float leadTime = 5f;
        [SerializeField, Range(0.1f, 0.5f)] private float passWindow = 0.22f;

        private static readonly string[] Commands =
        {
            "armLeft", "armRight", "legLeft", "legRight",
            "moveUp", "moveDown", "moveLeft", "moveRight",
            "salute", "croak"
        };
        private static readonly string[] Labels =
        {
            "左手", "右手", "左腿", "右腿",
            "↑", "↓", "←", "→", "敬礼", "呱叫"
        };

        public void Configure(RectTransform marker, Image markerImage,
            RectTransform[] slots, Text[] labels, Image[] images)
        {
            targetMarker = marker;
            targetImage = markerImage;
            noteSlots = slots;
            noteLabels = labels;
            noteImages = images;
        }

        private void Awake()
        {
            SetAllNotesActive(false);
        }

        public void Apply(GameStateData game)
        {
            if (game == null || noteSlots == null) return;
            if (GameSimulation.IsDanceSequenceActive(game))
            {
                ApplyDanceSequence(game);
                return;
            }
            var beats = CadenceBeatTable.Points;
            RectTransform track = (RectTransform)transform;
            Rect area = track.rect;
            float targetX = area.xMin + area.width * 0.085f;
            float spawnX = area.xMax - 48f;
            float laneY = 0f;
            int commandIndex = Mathf.Clamp(game.nextCadenceBeat, 0, beats.Count);
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
                    noteSlots[slot].gameObject.SetActive(false);
                    continue;
                }

                float remaining = beats[commandIndex].time + cycleOffset -
                                  game.musicTime;
                if (remaining > leadTime || remaining < -passWindow)
                {
                    noteSlots[slot].gameObject.SetActive(false);
                    commandIndex++;
                    continue;
                }

                string command = game.cadenceCommands[commandIndex];
                int kind = System.Array.IndexOf(Commands, command);
                if (kind < 0) kind = 0;
                float travel = Mathf.Clamp01(remaining / leadTime);
                float x = Mathf.Lerp(targetX, spawnX, travel);
                float hit = 1f - Mathf.Clamp01(Mathf.Abs(remaining) / 0.16f);
                targetPulse |= hit > 0f;

                RectTransform root = noteSlots[slot];
                root.gameObject.SetActive(true);
                root.anchoredPosition = new Vector2(x, laneY);
                root.localScale = Vector3.one * Mathf.Lerp(1f, 1.2f, hit);
                noteLabels[slot].text = Labels[kind];
                Color baseColor = CommandColor(kind);
                noteImages[slot].color = Color.Lerp(baseColor,
                    CampUiFactory.Hex("#F0A35B"), hit);
                noteLabels[slot].color = Color.Lerp(
                    CampUiFactory.White, Color.white, hit);
                commandIndex++;
            }

            if (targetMarker != null)
                targetMarker.localScale = Vector3.one *
                    (targetPulse ? 1.16f : 1f);
            if (targetImage != null)
                targetImage.color = targetPulse
                    ? CampUiFactory.Hex("#F0A35B")
                    : new Color(CampUiFactory.Accent.r,
                        CampUiFactory.Accent.g, CampUiFactory.Accent.b, 0.92f);
        }

        private void ApplyDanceSequence(GameStateData game)
        {
            if (game.specialMusicPhase == GameSimulation.DancePhaseWhistle)
            {
                SetAllNotesActive(false);
                SetTargetPulse(false);
                return;
            }
            if (game.specialMusicPhase == GameSimulation.DancePhasePause)
            {
                SetAllNotesActive(false);
                SetTargetPulse(false);
                return;
            }

            float danceTime = game.specialMusicPhase == GameSimulation.DancePhaseBell
                ? game.specialMusicTime - GameSimulation.BellSoundDuration
                : game.specialMusicTime;
            int commandIndex = Mathf.Max(0, game.nextDanceBeat - 1);
            Rect area = ((RectTransform)transform).rect;
            float targetX = area.xMin + area.width * 0.085f;
            float spawnX = area.xMax - 48f;
            bool targetPulse = false;

            for (int slot = 0; slot < noteSlots.Length; slot++)
            {
                if (commandIndex < 0 ||
                    commandIndex >= GameSimulation.DanceActionCount ||
                    commandIndex >= game.danceCommands.Count)
                {
                    noteSlots[slot].gameObject.SetActive(false);
                    continue;
                }

                float beatTime = GameSimulation.DanceActionStartTime +
                                 commandIndex * GameSimulation.DanceActionInterval;
                float remaining = beatTime - danceTime;
                if (remaining > leadTime || remaining < -passWindow)
                {
                    noteSlots[slot].gameObject.SetActive(false);
                    commandIndex++;
                    continue;
                }

                string command = game.danceCommands[commandIndex];
                int kind = System.Array.IndexOf(Commands, command);
                if (kind < 0) kind = 0;
                float travel = Mathf.Clamp01(remaining / leadTime);
                float hit = 1f - Mathf.Clamp01(Mathf.Abs(remaining) / 0.16f);
                targetPulse |= hit > 0f;

                RectTransform root = noteSlots[slot];
                root.gameObject.SetActive(true);
                root.anchoredPosition = new Vector2(
                    Mathf.Lerp(targetX, spawnX, travel), 0f);
                root.localScale = Vector3.one * Mathf.Lerp(1f, 1.2f, hit);
                noteLabels[slot].text = Labels[kind];
                Color baseColor = CommandColor(kind);
                noteImages[slot].color = Color.Lerp(
                    baseColor, CampUiFactory.Hex("#F0A35B"), hit);
                noteLabels[slot].color = Color.Lerp(
                    CampUiFactory.White, Color.white, hit);
                commandIndex++;
            }

            SetTargetPulse(targetPulse);
        }

        private void SetTargetPulse(bool pulse)
        {
            if (targetMarker != null)
                targetMarker.localScale = Vector3.one * (pulse ? 1.16f : 1f);
            if (targetImage != null)
                targetImage.color = pulse
                    ? CampUiFactory.Hex("#F0A35B")
                    : new Color(CampUiFactory.Accent.r,
                        CampUiFactory.Accent.g, CampUiFactory.Accent.b, 0.92f);
        }

        public void ApplyEditorPreview()
        {
            if (noteSlots == null) return;
            Rect area = ((RectTransform)transform).rect;
            for (int index = 0; index < noteSlots.Length; index++)
            {
                noteSlots[index].gameObject.SetActive(index < 5);
                if (index >= 5) continue;
                noteSlots[index].anchoredPosition = new Vector2(
                    Mathf.Lerp(area.xMin + area.width * 0.24f,
                        area.xMax - 48f, index / 4f), 0f);
                noteSlots[index].localScale = Vector3.one;
                noteLabels[index].text = Labels[index];
                noteImages[index].color = CommandColor(index);
            }
        }

        private void SetAllNotesActive(bool active)
        {
            if (noteSlots == null) return;
            foreach (RectTransform slot in noteSlots)
                if (slot != null) slot.gameObject.SetActive(active);
        }

        private static Color CommandColor(int kind)
        {
            if (kind < 2) return CampUiFactory.Hex("#6F9E75");
            if (kind < 4) return CampUiFactory.Hex("#7DAE92");
            return CampUiFactory.Hex("#496F59");
        }
    }
}
