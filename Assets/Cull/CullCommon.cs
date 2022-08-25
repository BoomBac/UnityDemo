using UnityEngine;

public class CullCommon
{
    public struct Plane
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

    public struct BoundBox
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
            DebugDraw(col);
        }
        public void DebugDraw(Color col)
        {
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

    public bool BoundBoxIntersection(BoundBox large_ab, BoundBox small_ab)
    {
        return large_ab.max_.x >= small_ab.max_.x && large_ab.max_.y >= small_ab.max_.y && large_ab.max_.z >= small_ab.max_.z &&
            large_ab.min_.x <= small_ab.min_.x && large_ab.min_.y <= small_ab.min_.y && large_ab.min_.z <= small_ab.min_.z;
    }

    public static void GetVirwFrustumPlane(Camera camera, out Plane[] frustum_planes)
    {
        Vector3 cam_pos = camera.transform.position;
        float nf_hdis = (camera.nearClipPlane + camera.farClipPlane) * 0.5f;
        float half_va = camera.fieldOfView * 0.5f;
        float half_width = Mathf.Tan(half_va * Mathf.Deg2Rad) * camera.farClipPlane * camera.aspect;
        float half_ha = Mathf.Atan(half_width / camera.farClipPlane) * Mathf.Rad2Deg;

        Vector3 center_up = Quaternion.AngleAxis(-half_va, camera.transform.right) * camera.transform.forward;
        Vector3 center_bottom = Quaternion.AngleAxis(half_va, camera.transform.right) * camera.transform.forward;
        Vector3 center_right = Quaternion.AngleAxis(half_ha, camera.transform.up) * camera.transform.forward;
        Vector3 center_left = Quaternion.AngleAxis(-half_ha, camera.transform.up) * camera.transform.forward;
        frustum_planes = new Plane[6];
        frustum_planes[0].normal_ = -camera.transform.forward;
        frustum_planes[1].normal_ = camera.transform.forward;
        frustum_planes[2].normal_ = Vector3.Cross(center_up, camera.transform.right);
        frustum_planes[3].normal_ = Vector3.Cross(center_bottom, -camera.transform.right);
        frustum_planes[4].normal_ = Vector3.Cross(center_left, camera.transform.up);
        frustum_planes[5].normal_ = Vector3.Cross(center_right, -camera.transform.up);
        frustum_planes[0].ori_ = cam_pos + camera.transform.forward * camera.nearClipPlane;
        frustum_planes[1].ori_ = cam_pos + camera.transform.forward * camera.farClipPlane;
        frustum_planes[2].ori_ = cam_pos + center_up * nf_hdis / Mathf.Cos(half_va * Mathf.Deg2Rad);
        frustum_planes[3].ori_ = cam_pos + center_bottom * nf_hdis / Mathf.Cos(half_va * Mathf.Deg2Rad);
        frustum_planes[4].ori_ = cam_pos + center_left * nf_hdis / Mathf.Cos(half_ha * Mathf.Deg2Rad);
        frustum_planes[5].ori_ = cam_pos + center_right * nf_hdis / Mathf.Cos(half_ha * Mathf.Deg2Rad);
        //Debug 绘制法线视椎体六个面的法线
        for (int i = 0; i < 6; ++i)
        {
            frustum_planes[i].DebugDraw();
        }
        //绘制水平视角范围
        for (int i = 0; i < 6; ++i)
        {
            Debug.DrawLine(cam_pos, frustum_planes[4].ori_ * 2, Color.red);
            Debug.DrawLine(cam_pos, frustum_planes[5].ori_ * 2, Color.red);
        }
    }

    public static Plane[] GetVirwFrustumPlane(Camera camera)
    {
        Vector3 cam_pos = camera.transform.position;
        float nf_hdis = (camera.nearClipPlane + camera.farClipPlane) * 0.5f;
        float half_va = camera.fieldOfView * 0.5f;
        float half_width = Mathf.Tan(half_va * Mathf.Deg2Rad) * camera.farClipPlane * camera.aspect;
        float half_ha = Mathf.Atan(half_width / camera.farClipPlane) * Mathf.Rad2Deg;

        Vector3 center_up = Quaternion.AngleAxis(-half_va, camera.transform.right) * camera.transform.forward;
        Vector3 center_bottom = Quaternion.AngleAxis(half_va, camera.transform.right) * camera.transform.forward;
        Vector3 center_right = Quaternion.AngleAxis(half_ha, camera.transform.up) * camera.transform.forward;
        Vector3 center_left = Quaternion.AngleAxis(-half_ha, camera.transform.up) * camera.transform.forward;
        Plane[] frustum_planes = new Plane[6];
        frustum_planes[0].normal_ = -camera.transform.forward;
        frustum_planes[1].normal_ = camera.transform.forward;
        frustum_planes[2].normal_ = Vector3.Cross(center_up, camera.transform.right);
        frustum_planes[3].normal_ = Vector3.Cross(center_bottom, -camera.transform.right);
        frustum_planes[4].normal_ = Vector3.Cross(center_left, camera.transform.up);
        frustum_planes[5].normal_ = Vector3.Cross(center_right, -camera.transform.up);
        frustum_planes[0].ori_ = cam_pos + camera.transform.forward * camera.nearClipPlane;
        frustum_planes[1].ori_ = cam_pos + camera.transform.forward * camera.farClipPlane;
        frustum_planes[2].ori_ = cam_pos + center_up * nf_hdis / Mathf.Cos(half_va * Mathf.Deg2Rad);
        frustum_planes[3].ori_ = cam_pos + center_bottom * nf_hdis / Mathf.Cos(half_va * Mathf.Deg2Rad);
        frustum_planes[4].ori_ = cam_pos + center_left * nf_hdis / Mathf.Cos(half_ha * Mathf.Deg2Rad);
        frustum_planes[5].ori_ = cam_pos + center_right * nf_hdis / Mathf.Cos(half_ha * Mathf.Deg2Rad);
        return frustum_planes;
    }

    public static BoundBox CalculateWolrdBoundBox(Mesh mesh, Matrix4x4 world_mat)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var pos in mesh.vertices)
        {
            if (pos.x < min.x) min.x = pos.x;
            if (pos.y < min.y) min.y = pos.y;
            if (pos.z < min.z) min.z = pos.z;
            if (pos.x > max.x) max.x = pos.x;
            if (pos.y > max.y) max.y = pos.y;
            if (pos.z > max.z) max.z = pos.z;
        }
        min = world_mat.MultiplyPoint(min);
        max = world_mat.MultiplyPoint(max);
        return new BoundBox(min, max);
    }

    /// <summary>
    /// </summary>
    /// <returns>0为视椎体外，1为相交于视椎体，2在视椎体内</returns>
    public static int FrustumBoundIntersection(BoundBox aabb, Plane[] frustum_planes)
    {
        Vector3 min = aabb.min_;
        Vector3 max = aabb.max_;
        int res = 0;
        for (int i = 0; i < 6; ++i)
        {
            Vector3 normal = frustum_planes[i].normal_;
            Vector3 pos = frustum_planes[i].ori_;
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

    public static bool FrustumBoundGeneralIntersection(BoundBox aabb, Plane[] frustum_planes)
    {
        Vector3 center = (aabb.max_ + aabb.min_) / 2;
        Debug.DrawLine(center, center + Vector3.up * 2, Color.blue);
        for (int i = 0; i < 6; ++i)
        {
            Vector3 normal = frustum_planes[i].normal_;
            Vector3 pos = frustum_planes[i].ori_;
            //包围盒在平面外侧
            if (Vector3.Dot(center - pos, normal) > 0)  //最近点在外侧，包围盒就在外侧
                return false;
        }
        return true;
    }

}
