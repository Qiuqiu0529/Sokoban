using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ObjDataList_SO", menuName = "Scriptable Objects/ObjDataList_SO")]
public class ObjDataList_SO : SerializedScriptableObject
{
    public List<ObjData> objDataList = new List<ObjData>();

    public ObjData GetObjDetails(int objID)
    {
        return objDataList.Find(i => i.objID == objID);
    }

    public List<ObjData> GetObjsByType(ObjectType objType)
    {
        return objDataList.FindAll(i => i.objType == objType);
    }
}