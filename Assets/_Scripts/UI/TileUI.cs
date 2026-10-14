using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;


public class TileUI : MonoBehaviour
{
    public GridView tileListView;
    public List<TileData> tileConfigs;
    //public TileType tileType;
    public TileDataList_SO tileDataList;
    PlaceMgr placeMgr;

    private void Start()
    {
        tileListView.AddDataBinder<TileData, ImageButtonVisuals>(BindTile);
        tileListView.AddGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
        tileConfigs = tileDataList.tileDataList;
        tileListView.SetDataSource(tileConfigs);
        placeMgr=PlaceMgr.Instance;
    }
    private void OnDestroy()
    {
        tileListView.RemoveDataBinder<TileData, ImageButtonVisuals>(BindTile);
        tileListView.RemoveGestureHandler<Gesture.OnClick, ImageButtonVisuals>(HandleClicked);
    }

    private void BindTile(Data.OnBind<TileData> evt, ImageButtonVisuals visuals, int index)
    {
        TileData tile = evt.UserData;
        visuals.Label.Text = tile.tileName;
        visuals.Image.SetImage(tile.tileSprite);
    }

    private void HandleClicked(Gesture.OnClick evt, ImageButtonVisuals target, int index)
    {
        TileData item = tileConfigs[index];
        placeMgr.EditTileMode(item as TileData);
    }

}
