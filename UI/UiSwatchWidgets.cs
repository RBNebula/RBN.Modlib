namespace RBN.Modlib.UI;

public static class UiSwatchWidgets
{
    public static bool DrawColorSwatchButton(Color color, float size = 20f, float innerPadding = 2f)
    {
        return DrawSwatchButtonCore(color, size, innerPadding);
    }

    public static bool DrawHexColorSwatchButton(string? hex, float size = 20f, float innerPadding = 2f)
    {
        return DrawSwatchButtonCore(TryParseHexColor(hex, out var color) ? color : (Color?)null, size, innerPadding);
    }

    private static bool DrawSwatchButtonCore(Color? color, float size, float innerPadding)
    {
        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        GUI.Box(rect, GUIContent.none);

        if (color.HasValue)
        {
            var fill = new Rect(
                rect.x + innerPadding,
                rect.y + innerPadding,
                Mathf.Max(0f, rect.width - (innerPadding * 2f)),
                Mathf.Max(0f, rect.height - (innerPadding * 2f)));
            UiDrawUtils.DrawSolidRect(fill, new Color(color.Value.r, color.Value.g, color.Value.b, 1f));
        }

        if (Event.current.type == EventType.Repaint && rect.Contains(Event.current.mousePosition))
        {
            UiDrawUtils.DrawSolidRect(rect, new Color(1f, 1f, 1f, 0.08f));
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private static bool TryParseHexColor(string? hex, out Color color)
    {
        var trimmed = (hex ?? string.Empty).Trim().TrimStart('#');
        if (trimmed.Length == 3)
        {
            trimmed = string.Concat(trimmed[0], trimmed[0], trimmed[1], trimmed[1], trimmed[2], trimmed[2]);
        }

        if (trimmed.Length == 6 && ColorUtility.TryParseHtmlString("#" + trimmed, out color))
        {
            return true;
        }

        color = default;
        return false;
    }
}
