using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DrawGrassInstance : MonoBehaviour
{
    private enum EFrustumPlane
    {
        kNear, kFar, kLeft, kRight, kTop, kBottom
    }

    private Dictionary<EFrustumPlane, Vector3> frustum_planes_ = new Dictionary<EFrustumPlane, Vector3>();
    public int InstanceNum = 1000;

    [Range(0, 20)]
    public float GrassRange = 20;

    List<Matrix4x4> transf;
    List<Vector3> bound_boxs;
    public Mesh mesh_grass;
    public Material mat_grass;
    public bool IsDraw = true;
    Camera main_cam_;

    /// <summary>
    /// http://imgtec.eetrend.com/blog/2021/100061097.html
    /// </summary>
    void GetVirwFrustumPlane()
    {
        frustum_planes_[EFrustumPlane.kNear] = -main_cam_.transform.forward;
        frustum_planes_[EFrustumPlane.kFar] = main_cam_.transform.forward;
        frustum_planes_[EFrustumPlane.kTop] = Vector3.Cross(Quaternion.AngleAxis(-main_cam_.fieldOfView * 0.5f, main_cam_.transform.right)
            * main_cam_.transform.forward, main_cam_.transform.right);
        frustum_planes_[EFrustumPlane.kBottom] = Vector3.Cross(Quaternion.AngleAxis(main_cam_.fieldOfView * 0.5f, main_cam_.transform.right)
    * main_cam_.transform.forward, -main_cam_.transform.right);
        Color normal_col = Color.cyan;
        Vector3 ori = main_cam_.transform.position;
        Debug.DrawLine(ori, frustum_planes_[EFrustumPlane.kNear] * 10 + ori, normal_col);
        Debug.DrawLine(ori, frustum_planes_[EFrustumPlane.kFar] * 10 + ori, normal_col);
        Debug.DrawLine(ori, frustum_planes_[EFrustumPlane.kTop] * 10 + ori, normal_col);
        Debug.DrawLine(ori, frustum_planes_[EFrustumPlane.kBottom] * 10 + ori, normal_col);
    }
    void DrawBoundBox(List<Vector3> pos, int id)
    {
        Debug.DrawLine(pos[id], pos[id + 1]);
        Debug.DrawLine(pos[id + 1], pos[id + 2]);
        Debug.DrawLine(pos[id + 2], pos[id + 3]);
        Debug.DrawLine(pos[id + 3], pos[id]);
        Debug.DrawLine(pos[id + 4], pos[id + 5]);
        Debug.DrawLine(pos[id + 5], pos[id + 6]);
        Debug.DrawLine(pos[id + 6], pos[id + 7]);
        Debug.DrawLine(pos[id + 7], pos[id + 4]);
        Debug.DrawLine(pos[id], pos[id + 4]);
        Debug.DrawLine(pos[id + 1], pos[id + 5]);
        Debug.DrawLine(pos[id + 2], pos[id + 6]);
        Debug.DrawLine(pos[id + 3], pos[id + 7]);
    }

    void CalculateBoundBox(int id)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var pos in mesh_grass.vertices)
        {
            if (pos.x < min.x) min.x = pos.x;
            if (pos.y < min.y) min.y = pos.y;
            if (pos.z < min.z) min.z = pos.z;
            if (pos.x > max.x) max.x = pos.x;
            if (pos.y > max.y) max.y = pos.y;
            if (pos.z > max.z) max.z = pos.z;
        }
        min = transf[id].MultiplyPoint(min);
        max = transf[id].MultiplyPoint(max);
        Vector3 n_lu = new Vector3(min.x, max.y, min.z);
        Vector3 n_ru = new Vector3(max.x, max.y, min.z);
        Vector3 n_rb = new Vector3(max.x, min.y, min.z);
        Vector3 f_lu = new Vector3(min.x, max.y, max.z);
        Vector3 f_lb = new Vector3(min.x, min.y, max.z);
        Vector3 f_rb = new Vector3(max.x, min.y, max.z);

        //Debug
        bound_boxs.Add(min);
        bound_boxs.Add(n_lu);
        bound_boxs.Add(n_ru);
        bound_boxs.Add(n_rb);
        bound_boxs.Add(f_lb);
        bound_boxs.Add(f_lu);
        bound_boxs.Add(max);
        bound_boxs.Add(f_rb);
    }

    void GenerateRandomPos()
    {
        transf.Clear();
        bound_boxs.Clear();
        for (int i = 0; i < InstanceNum; ++i)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            mat.SetTRS(new Vector3(Random.Range(-GrassRange, GrassRange), 0, Random.Range(-GrassRange, GrassRange)), Quaternion.identity, Vector3.one);
            transf.Add(mat);
        }
        for (int i = 0; i < InstanceNum; ++i)
            CalculateBoundBox(i);
    }

    // Start is called before the first frame update
    void Start()
    {
        main_cam_ = Camera.main;
        transf = new List<Matrix4x4>();
        bound_boxs = new List<Vector3>();
        GenerateRandomPos();
    }

    // Update is called once per frame
    void Update()
    {
        GetVirwFrustumPlane();
        for (int i = 0; i < bound_boxs.Count - 1; i += 8)
        {
            DrawBoundBox(bound_boxs, i);
        }
        Graphics.DrawMeshInstanced(mesh_grass, 0, mat_grass, transf);
    }
}
