using UnityEngine;

namespace MergeInstance
{
    public class ProcedualDraw : MonoBehaviour
    {
        public Mesh DMeshA;     //sphere
        public Mesh DMeshB;     //capsule
        public Material DMaterial;

        private Matrix4x4[] models;
        private ComputeBuffer instanceBuffer;
        private ComputeBuffer vertexBuffer;
        private ComputeBuffer indexBuffer;
        private const uint MESH_VERTEX_COUNT = 2496;
        private Bounds proxyBounds;
        private void Start()
        {
            proxyBounds = new Bounds(Vector3.zero, 100.0f * Vector3.one);
            InitMergeMesh();
            UpdateInstance();
        }
        private void InitMergeMesh()
        {
            var indexDataA = DMeshA.triangles;
            var indexDataB = DMeshB.triangles;
            var vertexDataA = DMeshA.vertices;
            var vertexDataB = DMeshB.vertices;
            var mergeIndexData = new int[MESH_VERTEX_COUNT * 2];
            for (int i = 0; i < indexDataA.Length; i++)
            {
                mergeIndexData[i] = indexDataA[i];
            }
            for (int i = indexDataA.Length; i < MESH_VERTEX_COUNT; i++)
            {
                mergeIndexData[i] = indexDataA[indexDataA.Length - 1];
            }
            for (int i = 0; i < indexDataB.Length; i++)
            {
                mergeIndexData[MESH_VERTEX_COUNT + i] = indexDataB[i] + vertexDataA.Length;
            }
            for (int i = (int)MESH_VERTEX_COUNT + indexDataB.Length; i < 2 * MESH_VERTEX_COUNT; i++)
            {
                mergeIndexData[i] = indexDataB[indexDataB.Length - 1] + vertexDataA.Length;
            }
            var mergeVertexData = new Vector3[vertexDataA.Length + vertexDataB.Length];
            for (int i = 0; i < vertexDataA.Length; i++)
            {
                mergeVertexData[i] = vertexDataA[i];
            }
            for (var i = 0; i < vertexDataB.Length; i++)
            {
                mergeVertexData[i + vertexDataA.Length] = vertexDataB[i];
            }
            vertexBuffer = new ComputeBuffer(mergeVertexData.Length, 3 * sizeof(float));
            vertexBuffer.name = nameof(vertexBuffer) + ":" + vertexBuffer.count;
            indexBuffer = new ComputeBuffer(mergeIndexData.Length, sizeof(int));
            indexBuffer.name = nameof(indexBuffer) + ":" + indexBuffer.count;
            vertexBuffer.SetData(mergeVertexData);
            indexBuffer.SetData(mergeIndexData);
            DMaterial.SetBuffer("_VertexBuffer", vertexBuffer);
            DMaterial.SetBuffer("_IndexBuffer", indexBuffer);
        }
        private void UpdateInstance()
        {
            models = new Matrix4x4[125 * 2];
            var parentPosition = transform.position;
            for (int i = 0, n = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    for (int k = 0; k < 5; k++, n++)
                    {
                        models[n] = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i - 5, j, k), Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.0f));
                        models[125 + n] = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i + 5, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
                    }
                }
            }
            var instanceParas = new InstancePara[125 * 2];
            for (int i = 0; i < 125 * 2; i++)
            {
                instanceParas[i].indexOffset = i < 125 ? 0 : MESH_VERTEX_COUNT;
                instanceParas[i].model = models[i];
                instanceParas[i].color = new Vector4(i < 125 ? Random.value : Random.Range(0.1f, 0.3f), Random.value, Random.Range(0.7f, 1.0f), 1);
            }
            if (instanceBuffer == null)
            {
                instanceBuffer = new ComputeBuffer(125 * 2, InstancePara.SIZE);
                instanceBuffer.name = nameof(instanceBuffer) + ":" + instanceBuffer.count;
                DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
            }
            instanceBuffer.SetData(instanceParas);
        }
        void Update()
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
            {
                UpdateInstance();
            }
            Graphics.DrawProcedural(DMaterial, proxyBounds, MeshTopology.Triangles, (int)MESH_VERTEX_COUNT, 125 * 2);
        }
        private void OnDestroy()
        {
            instanceBuffer?.Release();
            vertexBuffer?.Release();
            indexBuffer?.Release();
        }
        struct InstancePara
        {
            public uint indexOffset;
            public Matrix4x4 model;
            public Vector4 color;
            public const int SIZE = sizeof(uint) + 16 * sizeof(float) + 4 * sizeof(float);
        }
    }
}