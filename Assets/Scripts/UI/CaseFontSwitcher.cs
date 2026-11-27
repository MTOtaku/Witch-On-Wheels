using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Uses one font for UPPERCASE letters, another for lowercase letters, and a third
/// (fallback) for everything else. The fonts come from a shared CaseFontSettings asset.
/// The original text is never modified.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class CaseFontSwitcher : MonoBehaviour, ITextPreprocessor
{
    [Tooltip("Shared asset with the three fonts (Create > UI > Case Font Settings)")]
    [SerializeField] private CaseFontSettings settings;

    private const int None = 0, Upper = 1, Lower = 2, Fallback = 3;

    private TMP_Text tmp;
    private CaseFontSettings subscribedSettings;

    private void OnEnable()
    {
        tmp = GetComponent<TMP_Text>();

        if (settings == null)
            Debug.LogWarning("CaseFontSwitcher has no Settings asset assigned.", this);

        Subscribe();
        if (settings != null) settings.RegisterFonts();

        tmp.richText = true;
        tmp.textPreprocessor = this;
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (tmp == null) return;
        if (ReferenceEquals(tmp.textPreprocessor, this)) tmp.textPreprocessor = null;
        tmp.ForceMeshUpdate();
    }

    // Listens to the settings asset so editing it refreshes the text immediately
    private void Subscribe()
    {
        Unsubscribe();
        if (settings == null) return;
        settings.Changed += Refresh;
        subscribedSettings = settings;
    }

    private void Unsubscribe()
    {
        if (subscribedSettings != null) subscribedSettings.Changed -= Refresh;
        subscribedSettings = null;
    }

    private void Refresh()
    {
        if (tmp == null) return;
        tmp.havePropertiesChanged = true;
        tmp.ForceMeshUpdate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        Subscribe(); // the settings reference may have just been swapped
        if (settings != null) settings.RegisterFonts();
        if (tmp != null) tmp.havePropertiesChanged = true;
    }
#endif

    // Called by TMP every time it builds the text
    public string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text) || settings == null ||
            settings.UppercaseFont == null || settings.LowercaseFont == null)
            return text;

        var sb = new StringBuilder(text.Length * 2);
        int current = None;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // Copy existing rich text tags (<b>, <color=...>) untouched
            if (c == '<')
            {
                int end = text.IndexOf('>', i);
                if (end >= 0)
                {
                    sb.Append(text, i, end - i + 1);
                    i = end;
                    continue;
                }
            }

            // Whitespace stays in the current run to avoid pointless font switches
            if (char.IsWhiteSpace(c))
            {
                sb.Append(c);
                continue;
            }

            int kind = GetKind(c);

            // Only open a new font tag when the font actually changes
            if (kind != current)
            {
                if (current != None) sb.Append("</font>");
                sb.Append("<font=\"").Append(GetFont(kind).name).Append("\">");
                current = kind;
            }

            // The uppercase font may store its capital glyphs at the lowercase codes,
            // so send it the lowercase version of the letter
            sb.Append(kind == Upper ? ToUppercaseFontChar(c) : c);
        }

        if (current != None) sb.Append("</font>");
        return sb.ToString();
    }

    // Character that must be sent to the uppercase font for a given capital letter
    private char ToUppercaseFontChar(char c)
    {
        return settings.UppercaseFontUsesLowercaseCodes ? char.ToLowerInvariant(c) : c;
    }

    private int GetKind(char c)
    {
        // Check the glyph that will actually be sent to the uppercase font
        if (char.IsUpper(c) && settings.UppercaseFont.HasCharacter(ToUppercaseFontChar(c), false, true)) return Upper;
        if (char.IsLower(c) && settings.LowercaseFont.HasCharacter(c, false, true)) return Lower;

        // Numbers, symbols, uncased letters, or glyphs missing from their own font
        if (settings.FallbackFont != null) return Fallback;

        // No fallback assigned: letters keep their own font, the rest follows by case
        return char.IsUpper(c) ? Upper : Lower;
    }

    private TMP_FontAsset GetFont(int kind)
    {
        switch (kind)
        {
            case Upper: return settings.UppercaseFont;
            case Lower: return settings.LowercaseFont;
            default:    return settings.FallbackFont;
        }
    }
}
