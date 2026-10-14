using UnityEngine;
using NovaSamples.UIControls;
using UnityEngine.Events;
using Nova;

public class MyButton : UIControl<ButtonVisuals>
{
    public AudioClip uisound;

    [Tooltip("Event fired when the button is clicked.")]
    public UnityEvent OnClicked = null;

    protected void OnEnable()
    {

        if (View.TryGetVisuals(out ButtonVisuals visuals))
        {
            visuals.UpdateVisualState(VisualState.Default);
        }

        View.UIBlock.AddGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
        View.UIBlock.AddGestureHandler<Gesture.OnHover, ButtonVisuals>(ButtonVisuals.HandleHovered);
        View.UIBlock.AddGestureHandler<Gesture.OnUnhover, ButtonVisuals>(ButtonVisuals.HandleUnhovered);
        View.UIBlock.AddGestureHandler<Gesture.OnPress, ButtonVisuals>(ButtonVisuals.HandlePressed);
        View.UIBlock.AddGestureHandler<Gesture.OnRelease, ButtonVisuals>(ButtonVisuals.HandleReleased);
        View.UIBlock.AddGestureHandler<Gesture.OnCancel, ButtonVisuals>(ButtonVisuals.HandlePressCanceled);
    }
    public void Select()
    {
        if (View.TryGetVisuals(out ButtonVisuals visuals))
        {
            visuals.UpdateVisualState(VisualState.Pressed);
        }
    }
    public void Default()
    {
        if (View.TryGetVisuals(out ButtonVisuals visuals))
        {
            visuals.UpdateVisualState(VisualState.Default);
        }
    }


    protected void OnDisable()
    {
        View.UIBlock.RemoveGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
        View.UIBlock.RemoveGestureHandler<Gesture.OnHover, ButtonVisuals>(ButtonVisuals.HandleHovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ButtonVisuals>(ButtonVisuals.HandleUnhovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnPress, ButtonVisuals>(ButtonVisuals.HandlePressed);
        View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, ButtonVisuals>(ButtonVisuals.HandleReleased);
        View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, ButtonVisuals>(ButtonVisuals.HandlePressCanceled);
    }

    public virtual void RemoveClicked()
    {
        View.UIBlock.AddGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
    }


    protected void HandleClicked(Gesture.OnClick evt, ButtonVisuals visuals)
    {
        OnClicked?.Invoke();
        UISound.Instance.PlayUISound(uisound);
    }
}
