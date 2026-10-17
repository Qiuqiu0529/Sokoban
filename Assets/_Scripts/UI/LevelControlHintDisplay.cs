using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays level-specific tutorial text and selects the matching control hints
/// for the most recently used input device.
/// </summary>
public sealed class LevelControlHintDisplay : MonoBehaviour
{
    [Serializable]
    public sealed class LevelHint
    {
        [Tooltip("Level name or scene name, for example 1-1 or Level_1-1.")]
        public string levelName;

        [Tooltip("Enable separate keyboard and gamepad hint fields for this level.")]
        public bool useDeviceSpecificText;

        [TextArea(3, 8)]
        [Tooltip("Shown for both input devices when device-specific hints are disabled.")]
        public string text;

        [TextArea(3, 8)]
        [Tooltip("Keyboard hint shown when device-specific hints are enabled.")]
        public string keyboardText;

        [TextArea(3, 8)]
        [Tooltip("Gamepad hint shown when device-specific hints are enabled.")]
        public string gamepadText;
    }

    [SerializeField] private TMP_Text displayText;
    [SerializeField] private List<LevelHint> levelHints = new List<LevelHint>();
    [SerializeField] private Vector2 minimumTextSize = new Vector2(320f, 150f);
    [SerializeField] private float displayFontSize = 24f;

    private void OnEnable()
    {
        GameMgr.OnInputDeviceChanged += HandleInputDeviceChanged;
        StartCoroutine(RefreshAfterSceneInitialization());
    }

    private void OnDisable()
    {
        GameMgr.OnInputDeviceChanged -= HandleInputDeviceChanged;
    }

    private IEnumerator RefreshAfterSceneInitialization()
    {
        // GameMgr resolves levelName in Start, so wait until it has initialized.
        yield return null;
        if (displayText == null)
            displayText = GetComponentInChildren<TMP_Text>(true);

        if (displayText == null)
        {
            Debug.LogWarning($"[{nameof(LevelControlHintDisplay)}] No TMP text is assigned or found under {name}.", this);
            yield break;
        }

        Vector2 currentSize = displayText.rectTransform.sizeDelta;
        displayText.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(currentSize.x, minimumTextSize.x),
            Mathf.Max(currentSize.y, minimumTextSize.y));
        displayText.fontSize = displayFontSize;
        RefreshText();
    }

    private void HandleInputDeviceChanged(bool isGamepad)
    {
        RefreshText(isGamepad);
    }

    private void RefreshText(bool? isGamepadOverride = null)
    {
        if (displayText == null || levelHints == null || levelHints.Count == 0) return;

        LevelHint hint = FindHint(GameMgr.Instance != null ? GameMgr.Instance.levelName : null);
        if (hint == null)
            hint = FindHint(SceneManager.GetActiveScene().name);
        if (hint == null) return;

        bool isGamepad = isGamepadOverride ?? GameMgr.IsUsingGamepad;
        string text = hint.useDeviceSpecificText
            ? (isGamepad ? hint.gamepadText : hint.keyboardText)
            : hint.text;
        if (!string.IsNullOrWhiteSpace(text))
            displayText.text = text;
    }

    private LevelHint FindHint(string levelName)
    {
        string normalizedName = NormalizeLevelName(levelName);
        if (string.IsNullOrEmpty(normalizedName)) return null;

        foreach (LevelHint hint in levelHints)
        {
            if (hint != null && NormalizeLevelName(hint.levelName) == normalizedName)
                return hint;
        }

        return null;
    }

    private static string NormalizeLevelName(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName)) return string.Empty;

        string normalized = levelName.Trim();
        if (normalized.StartsWith("Level_", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("Level_".Length);
        else if (normalized.StartsWith("Level-", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("Level-".Length);
        else if (normalized.StartsWith("Level", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("Level".Length).TrimStart('_', '-');

        return normalized;
    }
}
