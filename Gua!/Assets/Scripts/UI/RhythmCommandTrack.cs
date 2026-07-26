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
        [SerializeField, Range(1.5f, 5f)] private float leadTime = 3f;
        [SerializeField, Range(0.1f, 0.5f)] private float passWindow = 0.22f;

        private static readonly string[] Commands =
        {
            "armLeft", "armRight", "legLeft", "legRight",
            "moveUp", "moveDown", "moveLeft", "moveRight"
        };
        private static readonly string[] Labels =
        {
            "左手", "右手", "左腿", "右腿",
            "↑", "↓", "←", "→"
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
            var beats = CadenceBeatTable.Points;
            RectTransform track = (RectTransform)transform;
            Rect area = track.rect;
            float targetX = area.xMin + area.width * 0.085f;
            float spawnX = area.xMax - 48f;
            float laneY = 0f;
            int first = 0;
            while (first < beats.Count &&
                   beats[first].time < game.musicTime - passWindow)
                first++;

            bool targetPulse = false;
            for (int slot = 0; slot < noteSlots.Length; slot++)
            {
                int commandIndex = first + slot;
                if (commandIndex >= beats.Count ||
                    commandIndex >= game.cadenceCommands.Count)
                {
                    noteSlots[slot].gameObject.SetActive(false);
                    continue;
                }

                float remaining = beats[commandIndex].time - game.musicTime;
                if (remaining > leadTime || remaining < -passWindow)
                {
                    noteSlots[slot].gameObject.SetActive(false);
                    continue;
                }

                string command = game.cadenceCommands[commandIndex];
                int kind = System.Array.IndexOf(Commands, command);
                if (kind < 0) kind = 0;
                float travel = Mathf.Clamp01(remaining / leadTime);
                float x = Mathf.Lerp(targetX, spawnX,
                    Mathf.SmoothStep(0f, 1f, travel));
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
