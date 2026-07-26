using UnityEngine;

namespace FrogCamp.UI
{
    public sealed class PondElementMotion : MonoBehaviour
    {
        public enum MotionKind
        {
            Sway,
            Float,
            Bloom,
            Feather,
            Insect,
            Fish
        }

        [SerializeField] private MotionKind motionKind;
        [SerializeField, Range(0.05f, 4f)] private float speed = 1f;
        [SerializeField] private float phase;
        [SerializeField] private Vector2 positionAmplitude =
            new Vector2(1f, 1f);
        [SerializeField, Range(0f, 20f)] private float rotationAmplitude = 2f;
        [SerializeField, Range(0f, 0.15f)] private float scaleAmplitude = 0.02f;
        [SerializeField] private bool flipWithDirection;

        private RectTransform target;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private float baseRotation;
        private bool captured;

        public void Configure(MotionKind kind, float motionSpeed,
            float motionPhase, Vector2 position, float rotation,
            float scale, bool flip)
        {
            motionKind = kind;
            speed = motionSpeed;
            phase = motionPhase;
            positionAmplitude = position;
            rotationAmplitude = rotation;
            scaleAmplitude = scale;
            flipWithDirection = flip;
        }

        private void OnEnable()
        {
            CaptureBase();
        }

        private void OnDisable()
        {
            RestoreBase();
        }

        private void CaptureBase()
        {
            target = transform as RectTransform;
            if (target == null) return;
            basePosition = target.anchoredPosition;
            baseScale = target.localScale;
            baseRotation = target.localEulerAngles.z;
            captured = true;
        }

        private void RestoreBase()
        {
            if (!captured || target == null) return;
            target.anchoredPosition = basePosition;
            target.localScale = baseScale;
            target.localEulerAngles =
                new Vector3(0f, 0f, baseRotation);
        }

        private void Update()
        {
            if (!Application.isPlaying || !captured || target == null)
                return;

            float time = Time.unscaledTime * speed + phase;
            float primary = Mathf.Sin(time);
            float secondary = Mathf.Sin(time * 1.73f + phase * 0.61f);
            Vector2 offset = Vector2.zero;
            float rotation = 0f;
            float scale = 1f;

            switch (motionKind)
            {
                case MotionKind.Sway:
                    offset = new Vector2(
                        secondary * positionAmplitude.x * 0.35f,
                        primary * positionAmplitude.y * 0.25f);
                    rotation = primary * rotationAmplitude;
                    scale = 1f + secondary * scaleAmplitude * 0.25f;
                    break;

                case MotionKind.Float:
                    offset = new Vector2(
                        secondary * positionAmplitude.x,
                        primary * positionAmplitude.y);
                    rotation = secondary * rotationAmplitude;
                    scale = 1f + primary * scaleAmplitude;
                    break;

                case MotionKind.Bloom:
                    offset = new Vector2(0f,
                        primary * positionAmplitude.y * 0.35f);
                    rotation = secondary * rotationAmplitude;
                    scale = 1f + (0.5f + 0.5f * primary) *
                        scaleAmplitude;
                    break;

                case MotionKind.Feather:
                    offset = new Vector2(
                        primary * positionAmplitude.x,
                        secondary * positionAmplitude.y);
                    rotation = primary * rotationAmplitude +
                        secondary * rotationAmplitude * 0.35f;
                    scale = 1f + secondary * scaleAmplitude;
                    break;

                case MotionKind.Insect:
                    offset = new Vector2(
                        primary * positionAmplitude.x,
                        Mathf.Sin(time * 1.47f + phase) *
                        positionAmplitude.y);
                    rotation = secondary * rotationAmplitude;
                    scale = 1f + Mathf.Abs(Mathf.Sin(time * 7.2f)) *
                        scaleAmplitude;
                    break;

                case MotionKind.Fish:
                    offset = new Vector2(
                        primary * positionAmplitude.x,
                        secondary * positionAmplitude.y);
                    rotation = secondary * rotationAmplitude;
                    scale = 1f + Mathf.Sin(time * 3.2f) *
                        scaleAmplitude;
                    break;
            }

            target.anchoredPosition = basePosition + offset;
            Vector3 nextScale = baseScale * scale;
            if (flipWithDirection &&
                (motionKind == MotionKind.Fish ||
                 motionKind == MotionKind.Insect))
            {
                float direction = Mathf.Cos(time);
                nextScale.x = Mathf.Abs(nextScale.x) *
                    (direction >= 0f ? 1f : -1f);
            }
            target.localScale = nextScale;
            target.localEulerAngles =
                new Vector3(0f, 0f, baseRotation + rotation);
        }
    }
}
