using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Core;
using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.UI.Screens;
using UnityEngine.EventSystems;

namespace TrackDynasty.Mvp03.UI
{
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.020f, 0.033f, 0.052f, 1f);
        public static readonly Color Panel = new Color(0.040f, 0.057f, 0.080f, 1f);
        public static readonly Color PanelAlt = new Color(0.060f, 0.080f, 0.108f, 1f);
        public static readonly Color Divider = new Color(0.13f, 0.17f, 0.22f, 1f);
        public static readonly Color Text = new Color(0.94f, 0.96f, 0.99f, 1f);
        public static readonly Color Muted = new Color(0.67f, 0.73f, 0.81f, 1f);
        public static readonly Color Gold = new Color(0.96f, 0.67f, 0.15f, 1f);
        public static readonly Color Green = new Color(0.42f, 0.88f, 0.22f, 1f);
        public static readonly Color GreenDark = new Color(0.08f, 0.19f, 0.11f, 1f);
        public static readonly Color Red = new Color(0.88f, 0.23f, 0.20f, 1f);
        public static readonly Color Track = new Color(0.49f, 0.21f, 0.14f, 1f);
        public static readonly Color LaneLine = new Color(0.92f, 0.92f, 0.90f, 0.75f);
    }
}

namespace TrackDynasty.Mvp03.UI
{
    public static class UIFactory
    {
        private static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static GameObject CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        public static Image Panel(Transform parent, Color color, string name = "Panel")
        {
            GameObject go = CreateRect(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Text(Transform parent, string value, int size = 18, TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null, FontStyle style = FontStyle.Normal, float height = 28f)
        {
            GameObject go = CreateRect("Text", parent);
            Text text = go.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color ?? UITheme.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return text;
        }

        public static Button Button(Transform parent, string label, UnityAction action, Color? color = null, float height = 46f, bool interactable = true)
        {
            GameObject go = CreateRect("Button_" + label, parent);
            Image image = go.AddComponent<Image>();
            image.color = color ?? UITheme.PanelAlt;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            if (action != null) button.onClick.AddListener(action);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            Text text = Text(go.transform, label, 16, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, height);
            RectTransform tr = text.rectTransform;
            Stretch(tr, 0, 0, 0, 0);
            return button;
        }

        public static Transform Vertical(Transform parent, float spacing = 8f, int padding = 0, string name = "Vertical")
        {
            GameObject go = CreateRect(name, parent);
            VerticalLayoutGroup group = go.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.transform;
        }

        public static Transform Horizontal(Transform parent, float spacing = 8f, float height = 46f, string name = "Horizontal")
        {
            GameObject go = CreateRect(name, parent);
            HorizontalLayoutGroup group = go.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return go.transform;
        }

        public static Transform ScrollContent(Transform parent, out ScrollRect scrollRect, int padding = 12)
        {
            GameObject scroll = CreateRect("ScrollView", parent);
            RectTransform scrollRt = Rect(scroll);
            Stretch(scrollRt, 0, 0, 0, 0);

            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.inertia = true;
            sr.scrollSensitivity = 28f;

            GameObject viewport = CreateRect("Viewport", scroll.transform);
            RectTransform viewportRt = Rect(viewport);
            Stretch(viewportRt, 0, 0, 0, 0);
            Image viewportGraphic = viewport.AddComponent<Image>();
            viewportGraphic.color = new Color(1f, 1f, 1f, 0.002f);
            viewportGraphic.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateRect("Content", viewport.transform);
            RectTransform contentRt = Rect(content);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.offsetMin = new Vector2(0f, 0f);
            contentRt.offsetMax = new Vector2(0f, 0f);
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 9f;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = viewportRt;
            sr.content = contentRt;
            sr.verticalNormalizedPosition = 1f;
            scrollRect = sr;
            return content.transform;
        }

        public static void BuildScreenError(Transform parent, Exception ex)
        {
            Image bg = Panel(parent, UITheme.Background, "ScreenBuildError");
            Stretch(bg.rectTransform, 0, 0, 0, 0);

            Transform stack = Vertical(bg.transform, 10f, 18, "ErrorStack");
            RectTransform rt = stack.GetComponent<RectTransform>();
            Stretch(rt, 0, 0, 0, 0);

            Text(stack, "SCREEN ERROR", 24, TextAnchor.MiddleLeft, UITheme.Red, FontStyle.Bold, 40f);
            Text(stack, "The screen failed to build. This message is shown instead of a blank page so the runtime error is visible immediately.", 15, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 62f);
            string message = ex == null ? "Unknown error" : ex.GetType().Name + ": " + ex.Message;
            Text(stack, message, 13, TextAnchor.UpperLeft, UITheme.Gold, FontStyle.Normal, 130f);
        }

        public static Image FixedPanel(Transform parent, Color color, float height, string name = "Card")
        {
            Image image = Panel(parent, color, name);
            LayoutElement le = image.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return image;
        }

        public static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static void SetFlexibleWidth(Component component, float value = 1f)
        {
            LayoutElement le = component.GetComponent<LayoutElement>();
            if (le == null) le = component.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = value;
            le.preferredWidth = -1f;
        }

        public static void SetPreferredWidth(Component component, float value)
        {
            LayoutElement le = component.GetComponent<LayoutElement>();
            if (le == null) le = component.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = value;
            le.flexibleWidth = 0f;
        }
    }
}

namespace TrackDynasty.Mvp03.UI
{
    public static class FlagSpriteFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string countryCode)
        {
            string code = string.IsNullOrEmpty(countryCode) ? "UNK" : countryCode.ToUpperInvariant();
            if (Cache.TryGetValue(code, out Sprite sprite)) return sprite;
            Texture2D texture = Build(code, 64);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[code] = sprite;
            return sprite;
        }

        private static Texture2D Build(string code, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color white = Color.white;
            Color red = new Color(0.82f, 0.05f, 0.07f);
            Color blue = new Color(0.04f, 0.18f, 0.43f);
            Color green = new Color(0.03f, 0.45f, 0.18f);
            Color yellow = new Color(0.98f, 0.82f, 0.10f);
            Color black = new Color(0.02f, 0.02f, 0.02f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    if (dx * dx + dy * dy > 0.94f) { tex.SetPixel(x, y, transparent); continue; }
                    float nx = x / (float)(size - 1);
                    float ny = y / (float)(size - 1);
                    Color c = new Color(0.25f, 0.28f, 0.34f);

                    if (code == "POL") c = ny > 0.5f ? white : red;
                    else if (code == "GER") c = ny > 0.666f ? black : ny > 0.333f ? red : yellow;
                    else if (code == "NGR") c = nx < 0.333f || nx > 0.666f ? green : white;
                    else if (code == "FRA") c = nx < 0.333f ? blue : nx < 0.666f ? white : red;
                    else if (code == "ITA") c = nx < 0.333f ? green : nx < 0.666f ? white : red;
                    else if (code == "CAN") c = nx < 0.25f || nx > 0.75f ? red : white;
                    else if (code == "JAM")
                    {
                        bool stripe = Mathf.Abs(ny - nx) < 0.10f || Mathf.Abs(ny - (1f - nx)) < 0.10f;
                        if (stripe) c = yellow;
                        else if ((ny < nx && ny < 1f - nx) || (ny > nx && ny > 1f - nx)) c = green;
                        else c = black;
                    }
                    else if (code == "USA")
                    {
                        c = Mathf.FloorToInt(ny * 13f) % 2 == 0 ? red : white;
                        if (nx < 0.43f && ny > 0.48f) c = blue;
                    }
                    else if (code == "GBR")
                    {
                        c = blue;
                        if (Mathf.Abs(nx - 0.5f) < 0.10f || Mathf.Abs(ny - 0.5f) < 0.10f) c = white;
                        if (Mathf.Abs(nx - 0.5f) < 0.045f || Mathf.Abs(ny - 0.5f) < 0.045f) c = red;
                    }
                    else if (code == "BRA")
                    {
                        c = green;
                        float diamond = Mathf.Abs(nx - 0.5f) / 0.34f + Mathf.Abs(ny - 0.5f) / 0.27f;
                        if (diamond < 1f) c = yellow;
                        float rx = (nx - 0.5f) / 0.14f;
                        float ry = (ny - 0.5f) / 0.14f;
                        if (rx * rx + ry * ry < 1f) c = blue;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
