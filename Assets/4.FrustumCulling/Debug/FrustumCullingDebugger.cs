using UnityEngine;

namespace FrustumCulling
{
    public class FrustumCullingDebugger : MonoBehaviour
    {
        public Camera DCamera;
        public Bounds DBounds;
        private Plane[] cullingPlanes = new Plane[6];

        void Update()
        {
            GeometryUtility.CalculateFrustumPlanes(DCamera, cullingPlanes);
        }
        private bool CPUFrustumCull()
        {
            var mt = this.transform.localToWorldMatrix.transpose;
            return PlaneTestBounds(DBounds, cullingPlanes, ref mt);
        }
        private bool PlaneTestBounds(Bounds boxLocal, Plane[] worldPlanes, ref Matrix4x4 mT)
        {
            foreach (var plane in worldPlanes)
            {
                var planeLocal = mT * new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
                Vector3 normalAbs = new Vector3(Mathf.Abs(planeLocal.x), Mathf.Abs(planeLocal.y), Mathf.Abs(planeLocal.z));
                float radius = Vector3.Dot(normalAbs, boxLocal.extents);
                float dist = Vector3.Dot(planeLocal, boxLocal.center) + planeLocal.w;
                if (radius + dist <= 0)
                {
                    return false;
                }
            }
            return true;
        }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.matrix = this.transform.localToWorldMatrix;
                Gizmos.color = CPUFrustumCull() ? Color.yellow : Color.red;
                Gizmos.DrawCube(DBounds.center, DBounds.size);
                Gizmos.color = Color.green;
                Gizmos.matrix = DCamera.transform.localToWorldMatrix;
                Gizmos.DrawFrustum(Vector3.zero, DCamera.fieldOfView, DCamera.farClipPlane, DCamera.nearClipPlane, DCamera.aspect);
            }
        }
    }
}