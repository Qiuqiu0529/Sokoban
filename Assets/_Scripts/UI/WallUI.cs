using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;

public class WallUI : MonoBehaviour
{
    public GridView tileListView;
    public List<WallData> tileConfigs;
    public WallDataList_SO tileDataList;
    PlaceMgr placeMgr;

    private void Start()
    {
        tileListView.AddDataBinder<WallData, ImageButtonVisuals>(BindWall);
        tileListView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
        tileConfigs = tileDataList.wallDataList;
        tileListView.SetDataSource(tileConfigs);
        placeMgr=PlaceMgr.Instance;
    }
    private void OnDestroy()
    {
        tileListView.RemoveDataBinder<WallData, ImageButtonVisuals>(BindWall);
        tileListView.RemoveGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }

    private void BindWall(Data.OnBind<WallData> evt, ImageButtonVisuals visuals, int index)
    {
        WallData tile = evt.UserData;
        visuals.Label.Text = tile.wallName;
        visuals.Image.SetImage(tile.wallSprite);
    }

    private void HandleClicked(Gesture.OnClick evt, ImageButtonVisuals target, int index)
    {
        WallData item = tileConfigs[index];
        placeMgr.EditWallMode(item as WallData);
    }

}
