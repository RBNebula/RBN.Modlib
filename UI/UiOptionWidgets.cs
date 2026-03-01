namespace RBN.Modlib.UI;

public readonly struct UiSegmentOption
{
    public UiSegmentOption(string label, float width = 0f)
    {
        Label = label ?? string.Empty;
        Width = width;
    }

    public string Label { get; }
    public float Width { get; }
}

public static class UiOptionWidgets
{
    public static int DrawSegmentedIndexRow(
        string label,
        int selectedIndex,
        GUIStyle labelStyle,
        GUIStyle? buttonStyle,
        float labelWidth,
        params UiSegmentOption[] options)
    {
        if (options == null || options.Length == 0) return selectedIndex;

        var nextIndex = Mathf.Clamp(selectedIndex, 0, options.Length - 1);
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(labelWidth));

        for (var i = 0; i < options.Length; i++)
        {
            var option = options[i];
            var text = i == nextIndex ? $"[{option.Label}]" : option.Label;
            var clicked = option.Width > 0f
                ? UiButtonUtils.DrawRichButton(text, buttonStyle, GUILayout.Width(option.Width))
                : UiButtonUtils.DrawRichButton(text, buttonStyle);
            if (clicked) nextIndex = i;
        }

        GUILayout.EndHorizontal();
        return nextIndex;
    }
}
