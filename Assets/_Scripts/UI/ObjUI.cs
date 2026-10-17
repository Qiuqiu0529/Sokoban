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
        if (objDataList == null) return;
        placeMgr=PlaceMgr.Instance;
        objConfigs = new List<ObjData>();
        foreach (ObjData data in objDataList.objDataList)
        {
            if (data == null) continue;
            if (!data.objType.HasFlag(ObjectType.PureDecoration)) objConfigs.Add(data);
        }

        if (objListView != null)
        {
            objListView.AddDataBinder<ObjData, ImageButtonVisuals>(BindObj);
            objListView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
            objListView.SetDataSource(objConfigs);
        }
    }
    private void OnDestroy()
    {
        if (objListView != null)
        {
            objListView.RemoveDataBinder<ObjData, ImageButtonVisuals>(BindObj);
            objListView.RemoveGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
        }
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
