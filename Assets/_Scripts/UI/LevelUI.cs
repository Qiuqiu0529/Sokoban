using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using System;

public class LevelUI : MonoBehaviour
{
    public GridView listView;
    public List<string> configs = new List<string>();

    private void Awake()
    {
        if (listView == null)
        {
            Debug.LogError($"[{nameof(LevelUI)}] No GridView is assigned.", this);
            return;
        }

        // The shared DecorationList prefab is configured for multiple columns.
        // Level names should always use a single vertical column.
        listView.CrossAxisItemCount = 1;
        listView.AddDataBinder<string, ImageButtonVisuals>(BindLevel);
        listView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }

    private void OnEnable()
    {
        // This object is toggled when switching editor panels. Reassert the
        // level-list layout on each activation because GridView can restore its
        // serialized layout state after being disabled/re-enabled.
        if (listView != null)
        {
            listView.CrossAxisItemCount = 1;
        }
    }


    public void SetList(List<string> strings)
    {
        configs = strings != null ? new List<string>(strings) : new List<string>();
        if (listView == null) return;

        // Reset the virtualized GridView before rebinding. The level list shares
        // its base prefab with multi-column object lists, and Nova retains
        // virtualized child blocks across data-source changes. Clearing first
        // avoids stale two-column slices being reused for the new level data.
        listView.SetDataSource<string>(null);
        listView.CrossAxisItemCount = 1;
        listView.SetDataSource(configs);
        listView.Relayout();
    }

    private void OnDestroy()
    {
        if (listView == null) return;
        listView.RemoveDataBinder<string, ImageButtonVisuals>(BindLevel);
        listView.RemoveGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }

    private void BindLevel(Data.OnBind<string> evt, ImageButtonVisuals visuals, int index)
    {
        string temp=evt.UserData;
        visuals.Label.Text = temp;
    }

    private void HandleClicked(Gesture.OnClick evt, ImageButtonVisuals target, int index)
    {
        if (index < 0 || index >= configs.Count || LevelEditor.Instance == null) return;
        string item = configs[index];
        LevelEditor.Instance.LoadLevel(item);
    }

}

