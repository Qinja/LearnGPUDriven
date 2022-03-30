using UnityEngine;

public class AdvancedMergeInstance : MonoBehaviour
{
    public Mesh DMeshA;     //sphere
    public Mesh DMeshB;     //capsule
    public Material DMaterial;

    private Matrix4x4[] models;
    private ComputeBuffer instanceBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
    private const uint MESH_VERTEX_COUNT = 2496;
    private Mesh proxyMesh;
    private void Start()
    {
        InitMergeMesh();
        InitProxyMesh();
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
        indexBuffer = new ComputeBuffer(mergeIndexData.Length, sizeof(int));
        vertexBuffer.SetData(mergeVertexData);
        indexBuffer.SetData(mergeIndexData);
        DMaterial.SetBuffer("_VertexBuffer", vertexBuffer);
        DMaterial.SetBuffer("_IndexBuffer", indexBuffer);
    }
    private void InitProxyMesh()
    {
        proxyMesh = new Mesh();
        var proxyVertexData = new Vector3[MESH_VERTEX_COUNT];
        var proxyIndexData = new int[MESH_VERTEX_COUNT];
        for (int i = 0; i < MESH_VERTEX_COUNT; i++)
        {
            proxyVertexData[i] = new Vector3(Random.value, Random.value, Random.value);
            proxyIndexData[i] = i;
        }
        proxyMesh.vertices = proxyVertexData;
        proxyMesh.triangles = proxyIndexData;
        proxyMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100.0f);
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
            instanceParas[i].color = new Vector4(i < 125 ? Random.value : Random.Range(0.1f, 0.6f), Random.value, Random.Range(0.2f, 1.0f), 1);
        }
        if (instanceBuffer == null)
        {
            instanceBuffer = new ComputeBuffer(125 * 2, InstancePara.SIZE);
            DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
        }
        instanceBuffer.SetData(instanceParas);
    }
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            UpdateInstance();
        }
        Graphics.DrawMeshInstanced(proxyMesh, 0, DMaterial, models);
    }
    private void OnDisable()
    {
        instanceBuffer?.Release();
        vertexBuffer?.Release();
        indexBuffer?.Release();
    }
    struct InstancePara
    {
        public uint indexOffset;
        public Vector4 color;
        public const int SIZE = sizeof(uint) + 4 * sizeof(float);
    }
}
