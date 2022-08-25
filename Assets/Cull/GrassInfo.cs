using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrassInfo", menuName = "Vegetation/GrassInfo")]  //创建快捷创建方法
public class GrassInfo : ScriptableObject
{
    [SerializeField]
    public Material GrassMat;
    public Mesh GrassMesh;

    [SerializeField]
    public List<Matrix4x4> GrassList;

    public int GrassLength;

    private void OnEnable()
    {
        Debug.Log("data lod");
    }
    private void OnDisable()
    {
        Debug.Log("data unload");
    }
}

