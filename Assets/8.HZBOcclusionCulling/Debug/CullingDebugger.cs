using UnityEngine;

namespace HZBOcclusionCulling
{
    public class CullingDebugger : MonoBehaviour
    {
        public HZBOcclusionCulling DHZBOcclusionCulling;
        public Material DMaterial;

        private int count = 0;
        private Bounds proxyBounds;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer instanceBuffer;
        void Start()
        {
            proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.name = "ArgsBuffer";
            UpdateInstance();
        }
        private void UpdateInstance()
        {
            if (count == DHZBOcclusionCulling.Count) return;
            count = DHZBOcclusionCulling.Count;
            var instanceParas = new InstancePara[count];
            var row = Mathf.FloorToInt(Mathf.Pow(count - 1, 1.0f / 3.0f)) + 1;
            var parentPosition = DHZBOcclusionCulling.transform.position;
            Random.InitState(0);
            for (int i = 0, n = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    for (int k = 0; k < row && n < count; k++, n++)
                    {
                        instanceParas[n].model = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
                        instanceParas[n].color = new Vector4(Random.Range(0.7f, 1.0f), Random.Range(0.1f, 0.6f), Random.value, 1);
                    }
                }
            }
            instanceBuffer?.Release();
            instanceBuffer = new ComputeBuffer(count, InstancePara.SIZE);
            instanceBuffer.name = nameof(instanceBuffer) + ":" + instanceBuffer.count;
            instanceBuffer.SetData(instanceParas);
            argsBuffer.SetData(new uint[5] { DHZBOcclusionCulling.DMesh.GetIndexCount(0), (uint)count, 0, 0, 0 });
            DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
        }
        void LateUpdate()
        {
            UpdateInstance();
            Graphics.DrawMeshInstancedIndirect(DHZBOcclusionCulling.DMesh, 0, DMaterial, proxyBounds, argsBuffer);
        }
        private void OnDestroy()
        {
            instanceBuffer?.Release();
            argsBuffer?.Release();
        }
        struct InstancePara
        {
            public Matrix4x4 model;
            public Vector4 color;
            public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
        }
    }
}