using UnityEngine;
using System.Linq;
using MicroCharacterController;
/// <summary>
/// 角色查看器
/// </summary>
public class BallViewer : BaseViewer
{

    public void Awake()
    {
        rootName = "BallViewRoot";
        isMulti = true;
    }

    // PreBall用roleId缓存
    public GameObject ViewUnit(string roleId,Transform point)
    {
        GameObject go =  base.TryPool(PoolType.Game,"RedBall","ball"+roleId);
        go.transform.position = point.position;
        go.transform.rotation = point.rotation;

        return go;
    }

}