using System;
using UnityEngine;

namespace FrogCamp.Gameplay
{
    [Serializable]
    public sealed class FrogAnimationSet
    {
        [SerializeField] private Texture2D idle;
        [SerializeField] private Texture2D hop;
        [SerializeField] private Texture2D jump;
        [SerializeField] private Texture2D armLeft;
        [SerializeField] private Texture2D armRight;
        [SerializeField] private Texture2D legLeft;
        [SerializeField] private Texture2D legRight;
        [SerializeField] private Texture2D croak;
        [SerializeField] private Texture2D tongue;
        [SerializeField] private Texture2D whistle;
        [SerializeField] private Texture2D salute;

        public Texture2D Idle { get { return idle; } }
        public Texture2D Hop { get { return hop; } }

        public void SetTextures(Texture2D idleTexture, Texture2D hopTexture,
            Texture2D jumpTexture, Texture2D armLeftTexture, Texture2D armRightTexture,
            Texture2D legLeftTexture, Texture2D legRightTexture,
            Texture2D croakTexture, Texture2D tongueTexture,
            Texture2D whistleTexture, Texture2D saluteTexture)
        {
            idle = idleTexture;
            hop = hopTexture;
            jump = jumpTexture;
            armLeft = armLeftTexture;
            armRight = armRightTexture;
            legLeft = legLeftTexture;
            legRight = legRightTexture;
            croak = croakTexture;
            tongue = tongueTexture;
            whistle = whistleTexture;
            salute = saluteTexture;
        }

        public Texture2D GetActionTexture(string action)
        {
            switch (action)
            {
                case "jump": return jump;
                case "armLeft": return armLeft;
                case "armRight": return armRight;
                case "legLeft": return legLeft;
                case "legRight": return legRight;
                case "croak": return croak;
                case "tongue": return tongue;
                case "whistle": return whistle;
                case "salute": return salute;
                default: return null;
            }
        }

        public static int GetFrameCount(string state)
        {
            switch (state)
            {
                case "jump":
                case "salute": return 8;
                case "whistle": return 7;
                case "armLeft":
                case "armRight": return 7;
                case "legLeft":
                case "legRight": return 5;
                default: return 6;
            }
        }

        private static readonly Vector2[] JumpFrameOffsets =
        {
            new Vector2(0f, 27.55f),
            new Vector2(0.32f, 32.99f),
            new Vector2(0f, 27.23f),
            new Vector2(-0.64f, 28.19f),
            new Vector2(0.64f, 19.86f),
            new Vector2(-0.96f, 24.02f),
            new Vector2(0.32f, 30.43f),
            new Vector2(0.32f, 27.87f)
        };

        public static Vector2 GetFrameOffset(string state, int frame)
        {
            const float tallFrameY = 27.55f;
            switch (state)
            {
                case "hop": return new Vector2(0f, 27.87f);
                case "idle": return Vector2.zero;
                case "jump":
                    return JumpFrameOffsets[Mathf.Clamp(frame, 0,
                        JumpFrameOffsets.Length - 1)];
                case "armLeft": return new Vector2(-9.93f, tallFrameY);
                case "armRight": return new Vector2(10.57f, tallFrameY);
                case "legLeft": return new Vector2(-8.97f, 27.87f);
                case "legRight": return new Vector2(8.65f, tallFrameY);
                default:
                    return new Vector2(0f, tallFrameY);
            }
        }
    }
}
