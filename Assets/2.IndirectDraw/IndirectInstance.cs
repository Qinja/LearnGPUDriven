using UnityEngine;

public class IndirectInstance : MonoBehaviour
{
    public Mesh DMesh;
    public Material DMaterial;
    public int Count;

    private Bounds proxyBounds;
    private ComputeBuffer indirectArgs;
    void Start()
    {
        proxyBounds = new Bounds(Vector3.zero, 100.0f * Vector3.one);
        indirectArgs = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);
        InitInstance();
    }
    void InitInstance()
    {
        if (Count < 1) Count = 1;
        indirectArgs?.SetData(new int[5] { (int)DMesh.GetIndexCount(0), Count, 0, 0, 0 });
        DMaterial.SetInt("_InstanceCount", Count);
        DMaterial.SetVector("_ParentPosition", transform.position);
    }
    void Update()
    {
        if(Input.GetKey(KeyCode.UpArrow))
        {
            Count++;
            InitInstance();
        }
        else if(Input.GetKey(KeyCode.DownArrow))
        {
            Count--;
            InitInstance();
        }
        Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial,proxyBounds, indirectArgs);
    }
    private void OnDisable()
    {
        indirectArgs.Release();
        indirectArgs.Dispose();
    }
}
