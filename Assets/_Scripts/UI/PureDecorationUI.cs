using System.Collections.Generic;
using Nova;
using NovaSamples.UIControls;
using UnityEngine;

public class PureDecorationUI : MonoBehaviour
{
    public GridView decorationListView;
    public ObjDataList_SO objDataList;

    private List<ObjData> decorationConfigs = new List<ObjData>();
    private PlaceMgr placeMgr;

    private void Start()
    {
        if (decorationListView == null || objDataList == null) return;

        placeMgr = PlaceMgr.Instance;
        decorationConfigs = new List<ObjData>();
        foreach (ObjData data in objDataList.objDataList)
        {
            if (data != null && data.objType.HasFlag(ObjectType.PureDecoration))
                decorationConfigs.Add(data);
        }

        decorationListView.AddDataBinder<ObjData, ImageButtonVisuals>(BindDecoration);
        decorationListView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
        decorationListView.SetDataSource(decorationConfigs);
    }

    private void OnDestroy()
    {
        if (decorationListView == null) return;
        decorationListView.RemoveDataBinder<ObjData, ImageButtonVisuals>(BindDecoration);
        decorationListView.RemoveGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }

    private void BindDecoration(Data.OnBind<ObjData> evt, ImageButtonVisuals visuals, int index)
    {
        ObjData decoration = evt.UserData;
        visuals.Label.Text = decoration.objName;
        visuals.Image.SetImage(decoration.objSprite);
    }

    private void HandleClicked(Gesture.OnClick evt, ImageButtonVisuals target, int index)
    {
        if (placeMgr == null || index < 0 || index >= decorationConfigs.Count) return;
        placeMgr.EditObjectMode(decorationConfigs[index]);
    }
}
