using UnityEngine;
using UnityEngine.Rendering;

namespace OcclusionCulling
{
    public class PreZWriteUAV : MonoBehaviour
    {
        public Mesh DMesh;
        public Camera DCamera;
        public Material DMaterial;
        public Material DMaterialPreZ;
        public int Count;

        private uint indexCount;
        private Bounds proxyBounds;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer visibilityBuffer;
        private ComputeBuffer visibilityFrameIndexBuffer;
        private ComputeBuffer instanceBuffer;
        private GraphicsBuffer preZCubeIndexBuffer;
        private CommandBuffer preZCommandBuffer;
        private int frameIndex;
        private uint[] visibilities;
        private bool occlusionVaild = false;
        private uint[] argsData;
        void Start()
        {
            proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.name = nameof(argsBuffer);
            indexCount = DMesh.GetIndexCount(0);
            argsData = new uint[5] { indexCount, 0, 0, 0, 0 };
            var indexData = new ushort[36] {
                1,0,3, 1,3,2,
                0,1,5, 0,5,4,
                0,4,7, 0,7,3,
                5,1,2, 5,2,6,
                7,6,2, 7,2,3,
                4,5,6, 4,6,7,
            };
            preZCubeIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexData.Length, sizeof(ushort));
            preZCubeIndexBuffer.SetData(indexData);
            DMaterialPreZ.SetVector("_BoundsExtent", DMesh.bounds.extents);
            DMaterialPreZ.SetVector("_BoundsCenter", DMesh.bounds.center);
            InitCommandBuffer();
            UpdateInstance();
        }
        private void InitCommandBuffer()
        {
            preZCommandBuffer = new CommandBuffer();
            preZCommandBuffer.name = gameObject.name;
            DCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, preZCommandBuffer);
        }
        private void UpdateCommandBuffer()
        {
            preZCommandBuffer.Clear();
            preZCommandBuffer.SetRandomWriteTarget(1, visibilityFrameIndexBuffer);
            preZCommandBuffer.DrawProcedural(preZCubeIndexBuffer, Matrix4x4.identity, DMaterialPreZ, 0, MeshTopology.Triangles, 36, Count);
            preZCommandBuffer.ClearRandomWriteTargets();
        }
        private void UpdateInstance()
        {
            if (Count < 1) Count = 1;
            var instanceParas = new InstancePara[Count];
            var row = Mathf.FloorToInt(Mathf.Pow(Count - 1, 1.0f / 3.0f)) + 1;
            var parentPosition = transform.position;
            Random.InitState(0);
            for (int i = 0, n = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    for (int k = 0; k < row && n < Count; k++, n++)
                    {
                        instanceParas[n].model = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
                        instanceParas[n].color = new Vector4(Random.Range(0.7f, 1.0f), Random.Range(0.1f, 0.6f), Random.value, 1);
                    }
                }
            }
            instanceBuffer?.Release();
            instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
            instanceBuffer.name = nameof(instanceBuffer) + ":" + instanceBuffer.count;
            instanceBuffer.SetData(instanceParas);
            visibilityBuffer?.Release();
            visibilityBuffer = new ComputeBuffer(Count, sizeof(uint));
            visibilityBuffer.name = nameof(visibilityBuffer) + ":" + visibilityBuffer.count;
            visibilityFrameIndexBuffer?.Release();
            visibilityFrameIndexBuffer = new ComputeBuffer(Count, sizeof(uint));
            visibilityFrameIndexBuffer.name = nameof(visibilityFrameIndexBuffer) + ":" + visibilityFrameIndexBuffer.count;
            visibilityFrameIndexBuffer.SetData(new uint[Count]);

            frameIndex = Mathf.Max(frameIndex, Count);
            visibilities = new uint[Count];
            occlusionVaild = false;
            DMaterialPreZ.SetBuffer("_InstanceBuffer", instanceBuffer);
            DMaterialPreZ.SetBuffer("_VisibilityFrameIndexBuffer", visibilityFrameIndexBuffer);
            DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
            DMaterial.SetBuffer("_VisibilityBuffer", visibilityBuffer);
            UpdateCommandBuffer();
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

            //prev frame
            uint visibleCount = 0;
            if (occlusionVaild)
            {
                visibilityFrameIndexBuffer.GetData(visibilities);
                for (uint i = 0; i < Count; i++)
                {
                    if (visibilities[i] == frameIndex - 1)
                    {
                        visibilities[visibleCount++] = i;
                    }
                }
            }
            else
            {
                for (; visibleCount < Count; visibleCount++)
                {
                    visibilities[visibleCount] = visibleCount;
                }
                occlusionVaild = true;
            }
            argsData[1] = visibleCount;
            visibilityBuffer.SetData(visibilities);
            argsBuffer.SetData(argsData);

            //current frame
            DMaterialPreZ.SetInt("_CurrentFrameIndex", frameIndex);
            Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
            frameIndex++;
        }
        private void OnDestroy()
        {
            visibilityBuffer?.Release();
            instanceBuffer?.Release();
            visibilityFrameIndexBuffer?.Release();
            argsBuffer?.Release();
            preZCubeIndexBuffer?.Release();
        }
        struct InstancePara
        {
            public Matrix4x4 model;
            public Vector4 color;
            public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
        }
    }
}