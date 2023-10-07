using UnityEngine;

namespace FrustumCulling
{
    public class CPUFrustumCulling : MonoBehaviour
    {
        public Camera DCamera;
        public Mesh DMesh;
        public Material DMaterial;
        public int Count;

        private InstancePara[] instanceParas;
        private Bounds proxyBounds;
        private ComputeBuffer instanceBuffer;
        private Plane[] cullingPlanes = new Plane[6];
        void Start()
        {
            proxyBounds = new Bounds(Vector3.zero, 100.0f * Vector3.one);
            UpdateInstance();
        }
        private void UpdateInstance()
        {
            if (Count < 1) Count = 1;
            instanceParas = new InstancePara[Count];
            var row = Mathf.FloorToInt(Mathf.Pow(Count - 1, 1.0f / 3.0f)) + 1;
            var parentPosition = transform.position;
            Random.InitState(0);
            for (int i = 0, n = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    for (int k = 0; k < row && n < Count; k++, n++)
                    {
                        instanceParas[n].model = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i, j, k), Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.0f));
                        instanceParas[n].color = new Vector4(Random.Range(0.3f, 0.6f), Random.value, Random.Range(0.7f, 1.0f), 1);
                    }
                }
            }
            instanceBuffer?.Release();
            instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
            instanceBuffer.name = nameof(instanceBuffer) + ":" + instanceBuffer.count;
            DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
        }
        void Update()
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                Count = Count + Mathf.Max(Count / 10, 1);
                UpdateInstance();
            }
            else if (Input.GetKey(KeyCode.DownArrow) && Count > 1)
            {
                Count = Count - Mathf.Max(Count / 10, 1);
                UpdateInstance();
            }
            GeometryUtility.CalculateFrustumPlanes(DCamera, cullingPlanes);
            var visibleCount = CPUFrustumCull();
            if (visibleCount > 0)
            {
                Graphics.DrawMeshInstancedProcedural(DMesh, 0, DMaterial, proxyBounds, visibleCount);
            }
        }
        private int CPUFrustumCull()
        {
            var instanceParasResult = new InstancePara[Count];
            var bounds = DMesh.bounds;
            int visibleCount = 0;
            for (int i = 0; i < Count; i++)
            {
                var mt = instanceParas[i].model.transpose;
                if (PlaneTestBounds(bounds, cullingPlanes, ref mt))
                {
                    instanceParasResult[visibleCount++] = instanceParas[i];
                }
            }
            if (visibleCount > 0)
            {
                instanceBuffer.SetData(instanceParasResult);
            }
            return visibleCount;
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
        private void OnDestroy()
        {
            instanceBuffer?.Release();
        }
        struct InstancePara
        {
            public Matrix4x4 model;
            public Vector4 color;
            public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
        }
    }
}