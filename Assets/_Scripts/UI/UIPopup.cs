using Nova;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reusable two-button popup. Put this component on the popup root and assign
/// its message TextBlock and the two MyButton components in the Inspector.
/// Each button's child Label TextBlock is found automatically when not assigned.
/// </summary>
public class UIPopup : MonoBehaviour
{
    private static UIPopup instance;
    public static UIPopup Instance
    {
        get
        {
            if (instance == null) instance = FindObjectOfType<UIPopup>(true);
            return instance;
        }
        private set => instance = value;
    }

    [SerializeField] private TextBlock messageText;
    [SerializeField] private MyButton confirmButton;
    [SerializeField] private TextBlock confirmLabel;
    [SerializeField] private MyButton cancelButton;
    [SerializeField] private TextBlock cancelLabel;
    [SerializeField] private bool hideOnAwake = true;

    private UnityAction confirmAction;
    private UnityAction cancelAction;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one UIPopup may be active in a scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ResolveLabelReferences();
        EnsureButtonEvents();

        if (confirmButton != null) confirmButton.OnClicked.AddListener(HandleConfirm);
        if (cancelButton != null) cancelButton.OnClicked.AddListener(HandleCancel);

        initialized = true;
        if (hideOnAwake) gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null && confirmButton.OnClicked != null)
            confirmButton.OnClicked.RemoveListener(HandleConfirm);
        if (cancelButton != null && cancelButton.OnClicked != null)
            cancelButton.OnClicked.RemoveListener(HandleCancel);
        if (Instance == this) Instance = null;
    }

    /// <summary>Show with the default button labels, "确定" and "取消".</summary>
    public void Show(string message, UnityAction onConfirm, UnityAction onCancel = null)
    {
        Show(message, "确定", onConfirm, "取消", onCancel);
    }

    /// <summary>Show with custom labels and callbacks for both buttons.</summary>
    public void Show(string message, string confirmText, UnityAction onConfirm,
        string cancelText, UnityAction onCancel)
    {
        Initialize();
        if (!ValidateReferences()) return;

        messageText.Text = message ?? string.Empty;
        confirmLabel.Text = confirmText ?? string.Empty;
        cancelLabel.Text = cancelText ?? string.Empty;
        confirmAction = onConfirm;
        cancelAction = onCancel;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        confirmAction = null;
        cancelAction = null;
        gameObject.SetActive(false);
    }

    private void HandleConfirm()
    {
        UnityAction action = confirmAction;
        Hide();
        action?.Invoke();
    }

    private void HandleCancel()
    {
        UnityAction action = cancelAction;
        Hide();
        action?.Invoke();
    }

    private void ResolveLabelReferences()
    {
        if (confirmLabel == null && confirmButton != null)
            confirmLabel = confirmButton.GetComponentInChildren<TextBlock>(true);
        if (cancelLabel == null && cancelButton != null)
            cancelLabel = cancelButton.GetComponentInChildren<TextBlock>(true);
    }

    private void EnsureButtonEvents()
    {
        if (confirmButton != null && confirmButton.OnClicked == null)
            confirmButton.OnClicked = new UnityEvent();
        if (cancelButton != null && cancelButton.OnClicked == null)
            cancelButton.OnClicked = new UnityEvent();
    }

    private bool ValidateReferences()
    {
        ResolveLabelReferences();
        if (messageText != null && confirmButton != null && confirmLabel != null &&
            cancelButton != null && cancelLabel != null)
            return true;

        Debug.LogError("UIPopup is missing a message TextBlock, button, or button Label TextBlock reference.", this);
        return false;
    }
}
