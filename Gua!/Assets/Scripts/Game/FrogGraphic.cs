using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.Gameplay
{
    public sealed class FrogGraphic : MaskableGraphic
    {
        private string role;
        private string action;
        private float progress;
        private bool moving;
        private bool stunned;
        private float animationTime;

        public void SetPose(string actorRole, string currentAction, float actionProgress,
            bool isMoving, bool isStunned, float time)
        {
            role = actorRole;
            action = currentAction;
            progress = actionProgress;
            moving = isMoving;
            stunned = isStunned;
            animationTime = time;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            bool officer = role == "officer";
            Color body = officer ? Hex("#D9A3AD") : Hex("#A8CA7E");
            Color shade = officer ? Hex("#B97886") : Hex("#7DA760");
            Color light = officer ? Hex("#EDC8CE") : Hex("#C8DDA6");
            Color outline = officer ? Hex("#604B50") : Hex("#435442");
            Color eye = Hex("#EEE4AF");
            float amount = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            float walk = moving ? Mathf.Sin(animationTime * 15f) * 1.4f : 0f;
            float leftArm = action == "armLeft" ? amount * 7f : Mathf.Max(0f, -walk);
            float rightArm = action == "armRight" ? amount * 7f : Mathf.Max(0f, walk);
            float leftLeg = action == "legLeft" ? amount * 7f : Mathf.Max(0f, walk);
            float rightLeg = action == "legRight" ? amount * 7f : Mathf.Max(0f, -walk);

            AddEllipse(mesh, new Vector2(0, -25), new Vector2(18, 5), Hex("#41634845"), 16);
            AddEllipse(mesh, new Vector2(-16 - leftLeg, -10), new Vector2(14 + leftLeg, 7), outline, 14);
            AddEllipse(mesh, new Vector2(16 + rightLeg, -10), new Vector2(14 + rightLeg, 7), outline, 14);
            AddEllipse(mesh, new Vector2(-16 - leftLeg, -10), new Vector2(12 + leftLeg, 5), shade, 14);
            AddEllipse(mesh, new Vector2(16 + rightLeg, -10), new Vector2(12 + rightLeg, 5), shade, 14);
            AddLine(mesh, new Vector2(-8, 8), new Vector2(-20 - leftArm, 20 + leftArm), 7, outline);
            AddLine(mesh, new Vector2(8, 8), new Vector2(20 + rightArm, 20 + rightArm), 7, outline);
            AddLine(mesh, new Vector2(-8, 8), new Vector2(-20 - leftArm, 20 + leftArm), 4, body);
            AddLine(mesh, new Vector2(8, 8), new Vector2(20 + rightArm, 20 + rightArm), 4, body);
            AddEllipse(mesh, Vector2.zero, new Vector2(14, 25), outline, 20);
            AddEllipse(mesh, new Vector2(0, 1), new Vector2(11.5f, 22.5f), body, 20);
            AddEllipse(mesh, new Vector2(-4, 3), new Vector2(2.2f, 10), new Color(light.r, light.g, light.b, .75f), 12);
            AddEllipse(mesh, new Vector2(-10, 17), new Vector2(6, 7.5f), outline, 16);
            AddEllipse(mesh, new Vector2(10, 17), new Vector2(6, 7.5f), outline, 16);
            AddEllipse(mesh, new Vector2(-10, 17), new Vector2(4.2f, 5.7f), eye, 16);
            AddEllipse(mesh, new Vector2(10, 17), new Vector2(4.2f, 5.7f), eye, 16);
            AddEllipse(mesh, new Vector2(-10, 18), new Vector2(1.6f, 2.8f), outline, 12);
            AddEllipse(mesh, new Vector2(10, 18), new Vector2(1.6f, 2.8f), outline, 12);

            if (action == "croak")
                AddEllipse(mesh, new Vector2(0, 19), new Vector2(3 + amount * 3, 1.4f + amount * 3), outline, 14);
            if (action == "tongue")
            {
                float reach = 8f + amount * 38f;
                AddLine(mesh, new Vector2(0, 19), new Vector2(0, 19 + reach), 5.5f, outline);
                AddLine(mesh, new Vector2(0, 19), new Vector2(0, 19 + reach), 3.2f, Hex("#D98691"));
                AddEllipse(mesh, new Vector2(0, 19 + reach), new Vector2(3.2f, 4.2f), Hex("#E6A0A8"), 12);
            }
            if (action == "whistle")
            {
                AddEllipse(mesh, new Vector2(0, 24), new Vector2(4, 5), Hex("#D9B75F"), 10);
                for (int i = 0; i < 3; i++)
                    AddRing(mesh, new Vector2(0, 26), 8 + ((progress * 3f + i) % 1f) * 14f,
                        1.4f, Hex("#FFF0A8"));
            }
            if (stunned)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = animationTime * 4f + i * Mathf.PI * 2f / 3f;
                    AddEllipse(mesh, new Vector2(Mathf.Cos(angle) * 22f,
                        2 + Mathf.Sin(angle) * 7f), new Vector2(3, 3), Hex("#F3D46F"), 8);
                }
            }
        }

        private static void AddEllipse(VertexHelper mesh, Vector2 center, Vector2 radius,
            Color color, int segments)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * radius.x,
                    Mathf.Sin(angle) * radius.y), color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        private static void AddLine(VertexHelper mesh, Vector2 from, Vector2 to,
            float width, Color color)
        {
            Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized * width * .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(from - normal, color, Vector2.zero);
            mesh.AddVert(from + normal, color, Vector2.zero);
            mesh.AddVert(to + normal, color, Vector2.zero);
            mesh.AddVert(to - normal, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddRing(VertexHelper mesh, Vector2 center, float radius,
            float width, Color color)
        {
            const int segments = 18;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                AddLine(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, color);
            }
        }

        private static Color Hex(string value)
        {
            Color result;
            return ColorUtility.TryParseHtmlString(value, out result) ? result : Color.white;
        }
    }
}
