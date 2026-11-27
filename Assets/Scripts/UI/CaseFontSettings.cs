using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Shared font configuration for every CaseFontSwitcher.
/// Change a font here and all texts using this asset update at once.
/// Create it with: Right-click in Project > Create > UI > Case Font Settings
/// </summary>
[CreateAssetMenu(fileName = "CaseFontSettings", menuName = "UI/Case Font Settings")]
public class CaseFontSettings : ScriptableObject
{
    [SerializeField] private TMP_FontAsset uppercaseFont;
    [Tooltip("Enable if the Uppercase Font's capital-looking glyphs are stored at the lowercase character codes (a-z). " +
             "The text then sends the lowercase letter to this font.")]
    [SerializeField] private bool uppercaseFontUsesLowercaseCodes = true;
    [SerializeField] private TMP_FontAsset lowercaseFont;
    [Tooltip("Used for digits, symbols, punctuation and any glyph the two fonts above don't have")]
    [SerializeField] private TMP_FontAsset fallbackFont;

    public bool UppercaseFontUsesLowercaseCodes => uppercaseFontUsesLowercaseCodes;
    public TMP_FontAsset UppercaseFont => uppercaseFont;
    public TMP_FontAsset LowercaseFont => lowercaseFont;
    public TMP_FontAsset FallbackFont => fallbackFont;

    /// <summary>Raised when a value changes in the Inspector, so texts can refresh.</summary>
    public event Action Changed;

    /// <summary>
    /// Registers the fonts so the TMP tag finds them by name,
    /// even if they are not inside a Resources folder.
    /// </summary>
    public void RegisterFonts()
    {
        if (uppercaseFont != null) MaterialReferenceManager.AddFontAsset(uppercaseFont);
        if (lowercaseFont != null) MaterialReferenceManager.AddFontAsset(lowercaseFont);
        if (fallbackFont != null) MaterialReferenceManager.AddFontAsset(fallbackFont);
    }

    private void OnEnable() => RegisterFonts();

#if UNITY_EDITOR
    private void OnValidate()
    {
        RegisterFonts();
        Changed?.Invoke();
    }
#endif
}
