using System.Collections.Generic;
using UnityEngine;
using static CullCommon;

//[ExecuteInEditMode]
public class DrawGrassInstance : MonoBehaviour
{
    public GrassInfo grass_info;

    bool GrassListDirty = true;

    [Range(0, 100)]
    public float LODDistance;

    [Range(0, 100)]
    public float CullDistanceThreshold;

    [Range(0, 180)]
    public float CullAngleThreshold;

    public struct GrassBatch
    {
        public GrassBatch(int length)
        {
            transf = new Matrix4x4[1000];
        }
        public Matrix4x4[] transf;
    }

    List<GrassBatch> batch_list;


    struct QuadNode
    {
        public QuadNode(Vector3 min, Vector3 max)
        {
            aabb_ = new BoundBox(min, max, 3);
            childs_ = new QuadNode[4];
            has_childs = true;
            lod_ = 0;
        }
        public int lod_;
        public BoundBox aabb_;
        public QuadNode[] childs_;
        public bool has_childs;
    }
    QuadNode root_;

    Vector3[] frustum_vert;

    private CullCommon.Plane[] frustum_planes_;

    int InstanceNum = 1000;
    float GrassRange = 20;

    List<Matrix4x4> transf;
    List<Matrix4x4> culled_transf;
    List<BoundBox> bound_boxs;
    //自下而上存储节点，当子节点相交时，不会存储其父节点
    List<QuadNode> grids_culled_;
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
        //最后的0.1为划分阈值，为0的话可能堆栈溢出
        float dis = Vector3.Distance(main_cam_.transform.position, center);
        if (dis > LODDistance)
            root.lod_ = 1;
        if (max_len - dis > 0.1f)
        {
            root.has_childs = true;
            root.childs_[0] = new QuadNode(left, far);
            root.childs_[1] = new QuadNode(center, max);
            root.childs_[2] = new QuadNode(min, center);
            root.childs_[3] = new QuadNode(near, right);
            for (int i = 0; i < 4; ++i)
            {
                GenerateQuadNode(ref root.childs_[i]);
                root.childs_[i].aabb_.DebugDraw(Color.white);
            }
        }
        else
            root.has_childs = false;
    }


    bool QuadCull(QuadNode node, Camera cam, float threshold)
    {
        bool bIntersect = false;
        Vector3 node_center = (node.aabb_.min_ + node.aabb_.max_) / 2;
        Vector3 cam_pos = cam.transform.position;
        float distance = Vector3.Distance(node_center, cam_pos);
        //在距离和夹角阈值之内的直接不进行剔除检测，直接为相交
        if (distance < threshold && Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.Normalize(node_center - cam_pos))) >= Mathf.Cos(CullAngleThreshold * Mathf.Deg2Rad))
            bIntersect = true;
        else
            bIntersect = FrustumBoundGeneralIntersection(node.aabb_, frustum_planes_);
        if (!bIntersect) return false;
        int child_insc_count = 0;
        if (node.has_childs)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (QuadCull(node.childs_[i], cam, threshold))
                    ++child_insc_count;
            }
        }
        //没有子节点相交时才将自身加入交叉节点
        if (child_insc_count == 0)
        {
            grids_culled_.Add(node);
        }
        return bIntersect;
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

        root_ = new QuadNode(min, max);
        GenerateQuadNode(ref root_);
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
            bound_boxs.Add(CalculateWolrdBoundBox(mesh_grass, transf[i]));
    }

    void StoreSceneGrassInfo()
    {
        if (grass_info)
        {
            grass_info.GrassList.Clear();
            var grass_group = GameObject.FindGameObjectWithTag("Grass");
            Transform[] transf = grass_group.GetComponentsInChildren<Transform>();
            for (int i = 0; i < transf.Length; ++i)
            {
                grass_info.GrassList.Add(transf[i].localToWorldMatrix);
            }
            grass_info.GrassLength = transf.Length;
            Debug.LogFormat("Grass List's lenght is {0}", transf.Length);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        main_cam_ = Camera.main;
        batch_list = new List<GrassBatch>();
        frustum_vert = new Vector3[4];
        transf = new List<Matrix4x4>();
        bound_boxs = new List<BoundBox>();
        grids_culled_ = new List<QuadNode>();
        GenerateRandomPos();
    }

    // Update is called once per frame
    void Update()
    {
        int grass_count = grass_info.GrassList.Count;
        if (GrassListDirty && grass_count > 0)
        {
            int batch_count = grass_count / 1000;
            int last_batch_num = grass_count % 1000;
            for (int i = 0; i < batch_count; ++i)
            {
                var batch = new GrassBatch(0);
                for (int j = 0; j < 1000; ++j)
                {
                    batch.transf[j] = grass_info.GrassList[i * 1000 + j];
                }
                batch_list.Add(batch);
            }
            GrassBatch last = new GrassBatch(0);
            for (int i = 0; i < last_batch_num; ++i)
                last.transf[i] = grass_info.GrassList[batch_count * 1000 + i];
            batch_list.Add(last);
            GrassListDirty = false;
        }
        for (int i = 0; i < batch_list.Count; ++i)
        {
            Graphics.DrawMeshInstanced(mesh_grass, 0, mat_grass, batch_list[i].transf);
        }

        //culled_transf.Clear();
        //grids_culled_.Clear();
        //GetVirwFrustumPlane(main_cam_, out frustum_planes_);
        //GenerateQuadTree();
        //QuadCull(root_, main_cam_, CullDistanceThreshold);
        //for (int i = 0; i < grids_culled_.Count; ++i)
        //    grids_culled_[i].aabb_.DebugDraw(Color.yellow);
        //for (int i = 0; i < bound_boxs.Count; ++i)
        //{
        //    FrustumBoundIntersection(bound_boxs[i], frustum_planes_);
        //}
        //Graphics.DrawMeshInstanced(mesh_grass, 0, mat_grass, transf);
    }
    //private void OnDrawGizmos()
    //{
    //    for (int i = 0; i < grids_culled_.Count; ++i)
    //    {
    //        if (grids_culled_[i].lod_ == 0)
    //            Gizmos.DrawIcon((grids_culled_[i].aabb_.max_ + grids_culled_[i].aabb_.min_) / 2, "lod0",false);
    //        else
    //            Gizmos.DrawIcon((grids_culled_[i].aabb_.max_ + grids_culled_[i].aabb_.min_) / 2, "lod1", false);
    //    }                
    //}
    private void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 100, 100), "StoreGrassInfo"))
            StoreSceneGrassInfo();
    }
}
