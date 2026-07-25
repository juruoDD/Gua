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

        public Texture2D Idle { get { return idle; } }
        public Texture2D Hop { get { return hop; } }

        public void SetTextures(Texture2D idleTexture, Texture2D hopTexture,
            Texture2D jumpTexture, Texture2D armLeftTexture, Texture2D armRightTexture,
            Texture2D legLeftTexture, Texture2D legRightTexture)
        {
            idle = idleTexture;
            hop = hopTexture;
            jump = jumpTexture;
            armLeft = armLeftTexture;
            armRight = armRightTexture;
            legLeft = legLeftTexture;
            legRight = legRightTexture;
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
                default: return null;
            }
        }

        public static int GetFrameCount(string state)
        {
            switch (state)
            {
                case "jump": return 8;
                case "armLeft":
                case "armRight": return 7;
                case "legLeft":
                case "legRight": return 5;
                default: return 6;
            }
        }
    }
}
