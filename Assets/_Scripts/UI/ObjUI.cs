using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;

public class ObjUI : MonoBehaviour
{
    public GridView objListView;
    public List<ObjData> objConfigs;
    //public ObjType objType;
    public ObjDataList_SO objDataList;
    PlaceMgr placeMgr;

    private void Start()
    {
        objListView.AddDataBinder<ObjData, ImageButtonVisuals>(BindObj);
        objListView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
        objConfigs = objDataList.objDataList;
        objListView.SetDataSource(objConfigs);
        placeMgr=PlaceMgr.Instance;
    }
    private void OnDestroy()
    {
        objListView.RemoveDataBinder<ObjData, ImageButtonVisuals>(BindObj);
        objListView.RemoveGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }

    private void BindObj(Data.OnBind<ObjData> evt, ImageButtonVisuals visuals, int index)
    {
        ObjData obj = evt.UserData;
        visuals.Label.Text = obj.objName;
        visuals.Image.SetImage(obj.objSprite);
    }

    private void HandleClicked(Gesture.OnClick evt, ImageButtonVisuals target, int index)
    {
        ObjData item = objConfigs[index];
        placeMgr.EditObjectMode(item as ObjData);
    }

}
