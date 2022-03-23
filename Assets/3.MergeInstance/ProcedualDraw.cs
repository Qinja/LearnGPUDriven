using UnityEngine;

public class ProcedualDraw : MonoBehaviour
{
    public Mesh DMeshA;     //sphere
    public Mesh DMeshB;     //capsule
    public Material DMaterial;

    private Matrix4x4[] models;
    private ComputeBuffer instanceBuffer;
    private ComputeBuffer vbobuffer;
    private ComputeBuffer ibobuffer;
    private const uint VSIZE = 2496;
    private Bounds proxyBounds;
    private void Start()
    {
        proxyBounds = new Bounds(Vector3.zero, 100.0f * Vector3.one);
        InitMergeMesh();
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
        vbobuffer = new ComputeBuffer(vbo.Length, 3 * sizeof(float));
        ibobuffer = new ComputeBuffer(ibo.Length, sizeof(int));
        vbobuffer.SetData(vbo);
        ibobuffer.SetData(ibo);
        DMaterial.SetBuffer("_VBO", vbobuffer);
        DMaterial.SetBuffer("_IBO", ibobuffer);
    }
    private void UpdateInstance()
    {
        models = new Matrix4x4[125 * 2];
        var parentPosition = transform.position;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 5; k++)
                {
                    models[25 * i + 5 * j + k] = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i - 5, j, k), Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.0f));
                    models[125 + 25 * i + 5 * j + k] = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i + 5, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
                }
            }
        }
        var instanceParas = new InstancePara[125 * 2];
        for (int i = 0; i < 125 * 2; i++)
        {
            instanceParas[i].index_offset = i < 125 ? 0 : VSIZE;
            instanceParas[i].model = models[i];
            instanceParas[i].color = new Vector4(i < 125 ? Random.value : Random.Range(0.1f, 0.3f), Random.value, Random.Range(0.7f, 1.0f), 1);
        }
        if(instanceBuffer == null)
        {
            instanceBuffer = new ComputeBuffer(125 * 2, InstancePara.SIZE);
            DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
        }
        instanceBuffer.SetData(instanceParas);
    }
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.R))
        {
            UpdateInstance();
        }
        Graphics.DrawProcedural(DMaterial, proxyBounds, MeshTopology.Triangles, (int)VSIZE, 125 * 2);
    }
    private void OnDisable()
    {
        instanceBuffer?.Release();
        vbobuffer?.Release();
        ibobuffer?.Release();
    }
    struct InstancePara
    {
        public uint index_offset;
        public Matrix4x4 model;
        public Vector4 color;
        public const int SIZE = sizeof(uint) + 16 * sizeof(float) + 4 * sizeof(float);
    }
}
