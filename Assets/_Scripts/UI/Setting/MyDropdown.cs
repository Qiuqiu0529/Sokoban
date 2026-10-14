using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NovaSamples.UIControls;
using UnityEngine.Events;
using Nova;

public class MyDropdown : UIControl<MyDropDownVisual>
{
    [Tooltip("The event fired when a new item is selected from the dropdown list.")]
    public UnityEvent<string> OnValueChanged = null;
    public UnityEvent<int> OnSelectedChanged = null;

    [SerializeField]
    [Tooltip("The data used to populate the list of selectable items in the dropdown.")]
    public DropdownData DropdownOptions = new DropdownData();

    /// <summary>
    /// The visuals associated with this dropdown control
    /// </summary>
    private MyDropDownVisual Visuals => View.Visuals as MyDropDownVisual;
    
    public TextMgr textMgr;
    
    public TextBlock labelBlock;
    public string labelKey;

    public void Expand()
    {
        // Tell the dropdown to expand, showing a list of
        // selectable options.
        Visuals.Expand(DropdownOptions);
    }

    public void Collapse()
    {
        // Collapse the dropdown and stop tracking it
        // as the expanded focused object.
        Visuals.Collapse();
    }

    private void OnEnable()
    {
        if (View.TryGetVisuals(out MyDropDownVisual visuals))
        {
            // Set default state
            visuals.UpdateVisualState(VisualState.Default);
        }

        // Subscribe to desired events
        View.UIBlock.AddGestureHandler<Gesture.OnHover, MyDropDownVisual>(MyDropDownVisual.HandleHovered);
        View.UIBlock.AddGestureHandler<Gesture.OnUnhover, MyDropDownVisual>(MyDropDownVisual.HandleUnhovered);
        View.UIBlock.AddGestureHandler<Gesture.OnPress, MyDropDownVisual>(MyDropDownVisual.HandlePressed);
        View.UIBlock.AddGestureHandler<Gesture.OnRelease, MyDropDownVisual>(MyDropDownVisual.HandleReleased);
        View.UIBlock.AddGestureHandler<Gesture.OnCancel, MyDropDownVisual>(MyDropDownVisual.HandlePressCanceled);
        View.UIBlock.AddGestureHandler<Gesture.OnClick, MyDropDownVisual>(HandleDropdownClicked);

        Visuals.OnValueChanged += HandleValueChanged;
        //InputManager.OnPostClick += HandlePostClick;
        textMgr=TextMgr.Instance;
        ChangeText();
        textMgr.changeText += ChangeText;

        // Ensure label is initialized
       
    }
    public void ChangeText()
    {
        labelBlock.Text= textMgr.GetText(labelKey);
        Visuals.InitSelectionLabel(DropdownOptions.CurrentSelection);
        //textBlock.text = TextMgr.Instance.GetText(textkey);
    }


    private void OnDisable()
    {
        // Unsubscribe from events
        View.UIBlock.RemoveGestureHandler<Gesture.OnHover, MyDropDownVisual>(MyDropDownVisual.HandleHovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, MyDropDownVisual>(MyDropDownVisual.HandleUnhovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnPress, MyDropDownVisual>(MyDropDownVisual.HandlePressed);
        View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, MyDropDownVisual>(MyDropDownVisual.HandleReleased);
        View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, MyDropDownVisual>(MyDropDownVisual.HandlePressCanceled);
        View.UIBlock.RemoveGestureHandler<Gesture.OnClick, MyDropDownVisual>(HandleDropdownClicked);

        Visuals.OnValueChanged -= HandleValueChanged;
        textMgr.changeText -= ChangeText;
        //InputManager.OnPostClick -= HandlePostClick;
    }

    /// <summary>
    /// Fire the Unity event when the selected value changes.
    /// </summary>
    /// <param name="value">The string in the list of selectable options.</param>
    private void HandleValueChanged(string value)
    {
        OnValueChanged?.Invoke(value);
        OnSelectedChanged?.Invoke(DropdownOptions.SelectedIndex);
    }

    /// <summary>
    /// Handle a <see cref="MyDropDownVisual"/> object in the <see cref="ListView">
    /// being clicked, and either expand or collapse it accordingly.
    /// </summary>
    /// <param name="evt">The click event data.</param>
    /// <param name="dropdownControl">The <see cref="ItemVisuals"/> object which was clicked.</param>
    private void HandleDropdownClicked(Gesture.OnClick evt, MyDropDownVisual dropdownControl)
    {
        if (evt.Receiver.transform.IsChildOf(dropdownControl.OptionsView.transform))
        {
            // The clicked object was not the dropdown itself but rather a list item within the dropdown.
            // The dropdownControl itself will handle this event, so we don't need to do anything here.
            return;
        }

        // Toggle the expanded state of the dropdown on click

        if (dropdownControl.IsExpanded)
        {
            Collapse();
        }
        else
        {
            Expand();
        }
    }

    /// <summary>
    /// Handles unfocusing the <see cref="Dropdown"/> if the user clicks somewhere else.
    /// </summary>
    private void HandlePostClick(UIBlock clickedUIBlock)
    {
        if (!Visuals.IsExpanded)
        {
            return;
        }

        if (clickedUIBlock == null || !clickedUIBlock.transform.IsChildOf(transform))
        {
            // Clicked somewhere else, so remove focus.
            Collapse();
        }
    }
}
