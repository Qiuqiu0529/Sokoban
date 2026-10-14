using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using System;

public class LevelUI : MonoBehaviour
{
    public GridView listView;
    public List<string> configs=new List<string>();
    void Awake()
    {
        listView.AddDataBinder<string, ImageButtonVisuals>(BindLevel);
        listView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }


    public void SetList(List<string> strings)
    {
        configs.Clear();
        configs=strings;
        listView.SetDataSource(configs);
    }
    private void OnDestroy()
    {
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
        string item = configs[index];
        LevelEditor.Instance.LoadLevel(item);
    }

}

