using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class DrawGrassInstance : MonoBehaviour
{
    struct Plane
    {
        public Plane(Vector3 ori, Vector3 normal)
        {
            ori_ = ori;
            normal_ = Vector3.Normalize(normal);
        }
        public void DebugDraw()
        {
            Debug.DrawLine(ori_, ori_ + normal_, Color.cyan);
        }
        public Vector3 ori_;
        public Vector3 normal_;
    }

    struct BoundBox
    {
        public BoundBox(Vector3 min, Vector3 max, int flag = 0)
        {
            flag_ = flag;
            min_ = min;
            max_ = max;
            points_ = new Vector3[8];
            points_[0] = min;
            points_[1] = new Vector3(min.x, max.y, min.z);
            points_[2] = new Vector3(max.x, max.y, min.z);
            points_[3] = new Vector3(max.x, min.y, min.z);
            points_[4] = new Vector3(min.x, min.y, max.z);
            points_[5] = new Vector3(min.x, max.y, max.z);
            points_[6] = max;
            points_[7] = new Vector3(max.x, min.y, max.z);
        }

        public void DebugDraw()
        {
            Color col = Color.white;
            if (flag_ == 1)
                col = Color.red;
            else if (flag_ == 2)
                col = Color.green;
            else if (flag_ == 3) //for quad node
                col = Color.yellow;

            Debug.DrawLine(points_[0], points_[1], col);
            Debug.DrawLine(points_[1], points_[2], col);
            Debug.DrawLine(points_[2], points_[3], col);
            Debug.DrawLine(points_[3], points_[0], col);

            Debug.DrawLine(points_[4], points_[5], col);
            Debug.DrawLine(points_[5], points_[6], col);
            Debug.DrawLine(points_[6], points_[7], col);
            Debug.DrawLine(points_[7], points_[4], col);

            Debug.DrawLine(points_[0], points_[4], col);
            Debug.DrawLine(points_[1], points_[5], col);
            Debug.DrawLine(points_[2], points_[6], col);
            Debug.DrawLine(points_[3], points_[7], col);
        }
        public Vector3 min_;
        public Vector3 max_;
        public int flag_;
        private Vector3[] points_;
    }

    struct QuadNode
    {
        public QuadNode(Vector3 min, Vector3 max)
        {
            aabb_ = new BoundBox(min, max, 3);
            childs_ = new QuadNode[4];
        }
        public BoundBox aabb_;
        public QuadNode[] childs_;
    }

    Vector3[] frustum_vert;

    private enum EFrustumPlane
    {
        kNear, kFar, kLeft, kRight, kTop, kBottom
    }

    //private Dictionary<EFrustumPlane, Vector3> frustum_planes_ = new Dictionary<EFrustumPlane, Vector3>();
    private Plane[] frustum_planes_ = new Plane[6];
    public int InstanceNum = 1000;

    [Range(0, 20)]
    public float GrassRange = 20;

    List<Matrix4x4> transf;
    List<BoundBox> bound_boxs;
    public Mesh mesh_grass;
    public Material mat_grass;
    public bool IsDraw = true;
    Camera main_cam_;

    void GenerateQuadNode(ref QuadNode root)
    {
        Vector3 min = root.aabb_.min_;
        Vector3 max = root.aabb_.max_;

        Vector3 center = (max + min) / 2;
        float max_len = max.x - min.x > max.z - min.z ? max.x - min.x : max.z - min.z;
        Vector3 left = new Vector3(min.x, min.y, center.z);
        Vector3 right = new Vector3(max.x, min.y, center.z);
        Vector3 far = new Vector3(center.x, min.y, max.z);
        Vector3 near = new Vector3(center.x, min.y, min.z);

        if (max_len - Vector3.Distance(main_cam_.transform.position, center) > 0.5f)
        {
            root.childs_[0] = new QuadNode(left, far);
            root.childs_[1] = new QuadNode(center, max);
            root.childs_[2] = new QuadNode(min, center);
            root.childs_[3] = new QuadNode(near, right);
            for (int i = 0; i < 4; ++i)
            {
                GenerateQuadNode(ref root.childs_[i]);
                root.childs_[i].aabb_.DebugDraw();
            }

        }
    }

    void GenerateQuadTree()
    {
        Vector3 cam_pos = main_cam_.transform.position;
        float nf_hdis = (main_cam_.nearClipPlane + main_cam_.farClipPlane) * 0.5f;
        float half_va = main_cam_.fieldOfView * 0.5f;
        float half_width = Mathf.Tan(half_va * Mathf.Deg2Rad) * main_cam_.farClipPlane * main_cam_.aspect;
        float half_ha = Mathf.Atan(half_width / main_cam_.farClipPlane) * Mathf.Rad2Deg;
        frustum_vert[0] = main_cam_.transform.forward * main_cam_.farClipPlane + cam_pos + -main_cam_.transform.right * (Mathf.Tan(half_ha * Mathf.Deg2Rad)) * main_cam_.farClipPlane;
        frustum_vert[1] = main_cam_.transform.forward * main_cam_.farClipPlane + cam_pos + main_cam_.transform.right * (Mathf.Tan(half_ha * Mathf.Deg2Rad)) * main_cam_.farClipPlane;
        frustum_vert[2] = main_cam_.transform.forward * main_cam_.nearClipPlane + cam_pos + -main_cam_.transform.right * (Mathf.Tan(half_ha * Mathf.Deg2Rad)) * main_cam_.nearClipPlane;
        frustum_vert[3] = main_cam_.transform.forward * main_cam_.nearClipPlane + cam_pos + main_cam_.transform.right * (Mathf.Tan(half_ha * Mathf.Deg2Rad)) * main_cam_.nearClipPlane;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var pos in frustum_vert)
        {
            if (pos.x < min.x) min.x = pos.x;
            if (pos.y < min.y) min.y = pos.y;
            if (pos.z < min.z) min.z = pos.z;
            if (pos.x > max.x) max.x = pos.x;
            if (pos.y > max.y) max.y = pos.y;
            if (pos.z > max.z) max.z = pos.z;
        }
        //视椎体平面线段
        //Debug.DrawLine(frustum_vert[0], frustum_vert[1], Color.red);
        //Debug.DrawLine(frustum_vert[2], frustum_vert[3], Color.red);
        //Debug.DrawLine(frustum_vert[0], frustum_vert[2], Color.red);
        //Debug.DrawLine(frustum_vert[1], frustum_vert[3], Color.red);

        QuadNode root = new QuadNode(min, max);
        GenerateQuadNode(ref root);

    }
    /// <summary>
    /// </summary>
    /// <returns>0为视椎体外，1为相交于视椎体，2在视椎体内</returns>
    int FrustumBoundIntersection(BoundBox aabb)
    {
        Vector3 min = aabb.min_;
        Vector3 max = aabb.max_;
        int res = 0;
        for (int i = 0; i < 6; ++i)
        {
            Vector3 normal = frustum_planes_[i].normal_;
            Vector3 pos = frustum_planes_[i].ori_;
            Vector3 p = min;
            Vector3 n = max;
            Vector3 center = (p + n) / 2;
            //包围盒在平面外侧
            if (Vector3.Dot(center - pos, normal) > 0)
            {
                if (normal.x <= 0)
                {
                    p.x = max.x;
                    n.x = min.x;
                }
                if (normal.y <= 0)
                {
                    p.y = max.y;
                    n.y = min.y;
                }
                if (normal.z <= 0)
                {
                    p.z = max.z;
                    n.z = min.z;
                }
            }
            else
            {
                if (normal.x >= 0)
                {
                    p.x = max.x;
                    n.x = min.x;
                }
                if (normal.y >= 0)
                {
                    p.y = max.y;
                    n.y = min.y;
                }
                if (normal.z >= 0)
                {
                    p.z = max.z;
                    n.z = min.z;
                }
            }
            //Debug.DrawLine(p, p + Vector3.up, Color.blue);
            //Debug.DrawLine(n, n + Vector3.up, Color.yellow);
            if (Vector3.Dot(p - pos, normal) > 0)  //最近点在外侧，包围盒就在外侧
            {
                res = 0;
                aabb.flag_ = res;
                return res;
            }  //外部
            else if (Vector3.Dot(p - pos, normal) < 0 && Vector3.Dot(n - pos, normal) < 0) //最近点和最远点都在平面内侧，整个包围盒都在视椎体内
            {
                if (res != 1) res = 2;
            }
            else res = 1; //远外近内，二者相交
            aabb.flag_ = res;
        }
        aabb.DebugDraw();
        return res;
    }
    void GetVirwFrustumPlane()
    {
        Vector3 cam_pos = main_cam_.transform.position;
        float nf_hdis = (main_cam_.nearClipPlane + main_cam_.farClipPlane) * 0.5f;
        float half_va = main_cam_.fieldOfView * 0.5f;
        float half_width = Mathf.Tan(half_va * Mathf.Deg2Rad) * main_cam_.farClipPlane * main_cam_.aspect;
        float half_ha = Mathf.Atan(half_width / main_cam_.farClipPlane) * Mathf.Rad2Deg;

        Vector3 center_up = Quaternion.AngleAxis(-half_va, main_cam_.transform.right) * main_cam_.transform.forward;
        Vector3 center_bottom = Quaternion.AngleAxis(half_va, main_cam_.transform.right) * main_cam_.transform.forward;
        Vector3 center_right = Quaternion.AngleAxis(half_ha, main_cam_.transform.up) * main_cam_.transform.forward;
        Vector3 center_left = Quaternion.AngleAxis(-half_ha, main_cam_.transform.up) * main_cam_.transform.forward;

        frustum_planes_[0].normal_ = -main_cam_.transform.forward;
        frustum_planes_[1].normal_ = main_cam_.transform.forward;
        frustum_planes_[2].normal_ = Vector3.Cross(center_up, main_cam_.transform.right);
        frustum_planes_[3].normal_ = Vector3.Cross(center_bottom, -main_cam_.transform.right);
        frustum_planes_[4].normal_ = Vector3.Cross(center_left, main_cam_.transform.up);
        frustum_planes_[5].normal_ = Vector3.Cross(center_right, -main_cam_.transform.up);
        frustum_planes_[0].ori_ = cam_pos + main_cam_.transform.forward * main_cam_.nearClipPlane;
        frustum_planes_[1].ori_ = cam_pos + main_cam_.transform.forward * main_cam_.farClipPlane;
        frustum_planes_[2].ori_ = cam_pos + center_up * nf_hdis / Mathf.Cos(half_va * Mathf.Deg2Rad);
        frustum_planes_[3].ori_ = cam_pos + center_bottom * nf_hdis / Mathf.Cos(half_va * Mathf.Deg2Rad);
        frustum_planes_[4].ori_ = cam_pos + center_left * nf_hdis / Mathf.Cos(half_ha * Mathf.Deg2Rad);
        frustum_planes_[5].ori_ = cam_pos + center_right * nf_hdis / Mathf.Cos(half_ha * Mathf.Deg2Rad);

        //绘制法线视椎体六个面的法线
        for (int i = 0; i < 6; ++i)
        {
            frustum_planes_[i].DebugDraw();
        }
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
        bound_boxs.Add(new BoundBox(min, max));
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
        frustum_vert = new Vector3[4];
        transf = new List<Matrix4x4>();
        bound_boxs = new List<BoundBox>();
        GenerateRandomPos();
    }

    // Update is called once per frame
    void Update()
    {
        GenerateQuadTree();
        GetVirwFrustumPlane();
        for (int i = 0; i < bound_boxs.Count; ++i)
        {
            FrustumBoundIntersection(bound_boxs[i]);
        }
        Graphics.DrawMeshInstanced(mesh_grass, 0, mat_grass, transf);
    }
}
