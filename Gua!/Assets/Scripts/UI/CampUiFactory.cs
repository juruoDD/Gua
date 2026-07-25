using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public static class CampUiFactory
    {
        public static readonly Color Page = Hex("#DFF1C7");
        public static readonly Color Paper = Hex("#F8F5DF");
        public static readonly Color Mint = Hex("#C6E7A7");
        public static readonly Color Accent = Hex("#91C96B");
        public static readonly Color Leaf = Hex("#527D58");
        public static readonly Color Deep = Hex("#294637");
        public static readonly Color Muted = Hex("#718274");
        public static readonly Color Line = Hex("#769773");
        public static readonly Color White = Hex("#FFFDF2");
        public static readonly Color Danger = Hex("#D77D68");

        private static Font font;

        public static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color, bool outline = false)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            gameObject.GetComponent<Image>().color = color;
            if (outline) AddOutline(gameObject, Line, new Vector2(3f, -3f));
            return rect;
        }

        public static Text Text(Transform parent, string name, string value, int size,
            Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax, TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            Text text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = GetFont();
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button Button(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            UnityAction onClick, bool primary = true)
        {
            Color baseColor = primary ? Accent : Paper;
            RectTransform rect = Panel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax,
                baseColor, true);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = primary ? Hex("#A9D985") : White;
            colors.pressedColor = primary ? Hex("#78AD58") : Mint;
            colors.disabledColor = Hex("#BBC8AD");
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            Text(rect, "Label", label, 25, Deep, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            return button;
        }

        public static InputField Input(Transform parent, string name, string placeholder,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            int characterLimit)
        {
            RectTransform rect = Panel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax,
                White, true);
            InputField input = rect.gameObject.AddComponent<InputField>();
            Text value = Text(rect, "Value", "", 23, Deep, Vector2.zero, Vector2.one,
                new Vector2(18f, 8f), new Vector2(-18f, -8f), TextAnchor.MiddleLeft);
            Text hint = Text(rect, "Placeholder", placeholder, 23, Muted, Vector2.zero, Vector2.one,
                new Vector2(18f, 8f), new Vector2(-18f, -8f), TextAnchor.MiddleLeft);
            hint.fontStyle = FontStyle.Italic;
            input.textComponent = value;
            input.placeholder = hint;
            input.characterLimit = characterLimit;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        public static Text ButtonLabel(Button button)
        {
            return button.transform.Find("Label").GetComponent<Text>();
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(value, out color) ? color : Color.white;
        }

        private static Font GetFont()
        {
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC",
                    "Source Han Sans SC", "Arial Unicode MS"
                }, 28);
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }
    }
}
