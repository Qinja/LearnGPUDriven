using UnityEngine;

public class MergeInstance : MonoBehaviour
{
    public Mesh DMeshA;     //sphere
    public Mesh DMeshB;     //capsule
    public Material DMaterial;

    private ComputeBuffer instanceBuffer;
    private ComputeBuffer vboBuffer;
    private ComputeBuffer iboBuffer;
    private const uint VSIZE = 2496;
    private Mesh proxyMesh;
    private void Start()
    {
        InitMergeMesh();
        UpdateProxyMesh();
        UpdateInstance();
    }

    private void InitMergeMesh()
    {
        var iboA = DMeshA.triangles;
        var iboB = DMeshB.triangles;
        var vboA = DMeshA.vertices;
        var vboB = DMeshB.vertices;
        var ibo = new int[VSIZE * 2];
        for (int i = 0; i < iboA.Length; i++)
        {
            ibo[i] = iboA[i];
        }
        for (int i = iboA.Length; i < VSIZE; i++)
        {
            ibo[i] = iboA[iboA.Length - 1];
        }
        for (int i = 0; i < iboB.Length; i++)
        {
            ibo[VSIZE + i] = iboB[i] + vboA.Length;
        }
        for (int i = (int)VSIZE + iboB.Length; i < 2 * VSIZE; i++)
        {
            ibo[i] = iboB[iboB.Length - 1] + vboA.Length;
        }
        var vbo = new Vector3[vboA.Length + vboB.Length];
        for (int i = 0; i < vboA.Length; i++)
        {
            vbo[i] = vboA[i];
        }
        for (var i = 0; i < vboB.Length; i++)
        {
            vbo[i + vboA.Length] = vboB[i];
        }
        vboBuffer = new ComputeBuffer(vbo.Length, 3 * sizeof(float));
        iboBuffer = new ComputeBuffer(ibo.Length, sizeof(int));
        vboBuffer.SetData(vbo);
        iboBuffer.SetData(ibo);
        DMaterial.SetBuffer("_VBO", vboBuffer);
        DMaterial.SetBuffer("_IBO", iboBuffer);
    }
    private void UpdateProxyMesh()
    {
        proxyMesh = new Mesh();
        var p_vbo = new Vector3[VSIZE * 125 * 2];
        var p_ibo = new int[VSIZE * 125 * 2];
        for (int i = 0; i < VSIZE * 125 * 2; i++)
        {
            p_vbo[i] = new Vector3(Random.value, Random.value, Random.value);
            p_ibo[i] = i;
        }
        if (VSIZE * 125 * 2 >= ushort.MaxValue)
        {
            proxyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        proxyMesh.vertices = p_vbo;
        proxyMesh.triangles = p_ibo;
        proxyMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100.0f);
    }
    private void UpdateInstance()
    {
        var models = new Matrix4x4[125 * 2];
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
            instanceParas[i].index_offset = i < 125 ? 0 : VSIZE;
            instanceParas[i].model = models[i];
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
            UpdateProxyMesh();
            UpdateInstance();
        }
        Graphics.DrawMesh(proxyMesh, Matrix4x4.identity, DMaterial, 0);
    }
    private void OnDisable()
    {
        instanceBuffer?.Release();
        vboBuffer?.Release();
        iboBuffer?.Release();
    }
    struct InstancePara
    {
        public uint index_offset;
        public Matrix4x4 model;
        public Vector4 color;
        public const int SIZE = sizeof(uint) + 16 * sizeof(float) + 4 * sizeof(float);
    }
}
