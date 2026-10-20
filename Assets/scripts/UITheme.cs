using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UITheme
{
    public static readonly Color Ink = new Color32(38, 23, 14, 255);
    public static readonly Color DeepBrown = new Color32(48, 29, 18, 248);
    public static readonly Color PanelBrown = new Color32(66, 41, 25, 252);
    public static readonly Color Cream = new Color32(255, 244, 224, 255);
    public static readonly Color MutedCream = new Color32(219, 195, 158, 255);
    public static readonly Color Gold = new Color32(217, 154, 78, 255);
    public static readonly Color LightGold = new Color32(243, 199, 122, 255);
    public static readonly Color Danger = new Color32(181, 74, 55, 255);
    public static readonly Color Dim = new Color32(20, 11, 6, 205);

    private static Font cachedFont;
    private static Sprite cachedRoundedSprite;

    public static Font Font
    {
        get
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }
    }

    public static Sprite RoundedSprite
    {
        get
        {
            if (cachedRoundedSprite == null)
            {
                cachedRoundedSprite = CreateRoundedSprite();
            }

            return cachedRoundedSprite;
        }
    }

    private static Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const int radius = 14;
        const float half = size * 0.5f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + 0.5f - half);
                float py = Mathf.Abs(y + 0.5f - half);
                float dx = Mathf.Max(px - (half - radius), 0f);
                float dy = Mathf.Max(py - (half - radius), 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(16f, 16f, 16f, 16f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    public static GameObject CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        return canvasObject;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystem);
    }

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    public static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static Image CreateImage(Transform parent, string name, Color color, bool raycastTarget)
    {
        RectTransform rect = CreateRect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Image CreatePanel(Transform parent, string name, Color color, bool raycastTarget)
    {
        Image image = CreateImage(parent, name, color, raycastTarget);
        if (RoundedSprite != null)
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    public static Image CreateFullScreenOverlay(Transform parent, string name, Color color, bool raycastTarget)
    {
        Image image = CreateImage(parent, name, color, raycastTarget);
        Stretch(image.rectTransform, 0f, 0f, 0f, 0f);
        return image;
    }

    public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(parent, name);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;
        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label, Vector2 size, Color background, Color textColor, int fontSize, UnityAction onClick)
    {
        Image image = CreatePanel(parent, name, background, true);
        image.rectTransform.sizeDelta = size;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);

        Text text = CreateText(image.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 14f, 14f, 4f, 4f);
        text.fontStyle = FontStyle.Bold;
        AddShadow(text.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(1f, -1f));

        return button;
    }

    public static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    public static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
        }
    }
}
