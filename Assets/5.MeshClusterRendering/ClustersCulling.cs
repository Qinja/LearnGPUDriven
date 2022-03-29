using UnityEngine;

public class ClustersCulling : MonoBehaviour
{
    public Camera DCamera;
    public ClustersData DClusters;
    public Material DMaterial;
    public int Count;
    public ComputeShader DComputeShader;

    private const int CLUSTER_VERTEX_COUNT = 64;
    private const int KERNEL_SIZE_X = 64;
    private int cullKernelID;
    private int kernelGroupX;
    private Bounds proxyBounds;
    private GraphicsBuffer indexBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer visibilityBuffer;
    private ComputeBuffer instanceBuffer;
    private ComputeBuffer vboBuffer;
    private ComputeBuffer boundsBuffer;
    private Plane[] cullingPlanes = new Plane[6];
    private Vector4[] cullingPlaneVectors = new Vector4[6];
    void Start()
    {
        proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        cullKernelID = DComputeShader.FindKernel("FrustumCullMain");
        DComputeShader.SetBuffer(cullKernelID, "_ArgsBuffer", argsBuffer);
        DComputeShader.SetInt("_ClusterCount", DClusters.Count);
        InitClusters();
        InitIndexBuffer();
        UpdateInstance();
    }
    private void InitIndexBuffer()
    {
        var indexLength = CLUSTER_VERTEX_COUNT / 4 * 6;
        var indexData = new ushort[indexLength];
        for (int i = 0; i < CLUSTER_VERTEX_COUNT / 4; i++)
        {
            indexData[i * 6 + 0] = (ushort)(i * 4 + 0);
            indexData[i * 6 + 1] = (ushort)(i * 4 + 1);
            indexData[i * 6 + 2] = (ushort)(i * 4 + 2);
            indexData[i * 6 + 3] = (ushort)(i * 4 + 0);
            indexData[i * 6 + 4] = (ushort)(i * 4 + 2);
            indexData[i * 6 + 5] = (ushort)(i * 4 + 3);
        }
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexLength, sizeof(ushort));
        indexBuffer.SetData(indexData);
    }
    private void InitClusters()
    {
        var vbo = DClusters.Vertices;
        vboBuffer = new ComputeBuffer(vbo.Length, 3 * sizeof(float));
        vboBuffer.SetData(vbo);
        DMaterial.SetBuffer("_VBO", vboBuffer);
        DMaterial.SetInteger("_ClusterCount", DClusters.Count);
        boundsBuffer = new ComputeBuffer(DClusters.Count, 2 * 3 * sizeof(float));
        boundsBuffer.SetData(DClusters.BoundingBoxes);
        DComputeShader.SetBuffer(cullKernelID, "_BoundingBoxes", boundsBuffer);
        DComputeShader.SetInt("_ClusterCount", DClusters.Count);
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
                    instanceParas[n].model = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i, j, k), Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.0f));
                    instanceParas[n].color = new Vector4(Random.Range(0.2f, 0.8f), Random.Range(0.5f, 1.0f), Random.value, 1);
                }
            }
        }
        instanceBuffer?.Release();
        instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
        instanceBuffer.SetData(instanceParas);
        visibilityBuffer?.Release();
        visibilityBuffer = new ComputeBuffer(Count * DClusters.Count, sizeof(uint));
        visibilityBuffer.SetData(new uint[Count * DClusters.Count]);

        DComputeShader.SetBuffer(cullKernelID, "_InstanceBuffer", instanceBuffer);
        DComputeShader.SetBuffer(cullKernelID, "_VisibilityBuffer", visibilityBuffer);
        DComputeShader.SetInt("_Count", Count);
        DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
        DMaterial.SetBuffer("_VisibilityBuffer", visibilityBuffer);
        kernelGroupX = Mathf.CeilToInt(1.0f * Count / KERNEL_SIZE_X);
    }
    private void UpdateCullingPlane()
    {
        GeometryUtility.CalculateFrustumPlanes(DCamera, cullingPlanes);
        for (int i = 0; i < 6; i++)
        {
            var normal = cullingPlanes[i].normal;
            cullingPlaneVectors[i] = new Vector4(normal.x, normal.y, normal.z, cullingPlanes[i].distance);
        }
        DComputeShader.SetVectorArray("_CullingPlanes", cullingPlaneVectors);
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Count = Count + Mathf.Max(Count / 10, 1);
            UpdateInstance();
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            Count = Count - Mathf.Max(Count / 10, 1);
            UpdateInstance();
        }
        UpdateCullingPlane();
        argsBuffer.SetData(new uint[5] { (uint)indexBuffer.count, 0, 0, 0, 0 });
        DComputeShader.Dispatch(cullKernelID, kernelGroupX, 1, 1);
        Graphics.DrawProceduralIndirect(DMaterial, proxyBounds, MeshTopology.Triangles, indexBuffer, argsBuffer);
    }
    private void OnDisable()
    {
        indexBuffer?.Release();
        argsBuffer?.Release();
        visibilityBuffer?.Release();
        instanceBuffer?.Release();
        vboBuffer?.Release();
        boundsBuffer?.Release();
    }
    struct InstancePara
    {
        public Matrix4x4 model;
        public Vector4 color;
        public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
    }
}