using UnityEngine;

public class InstanceBuffer : MonoBehaviour
{
    public Mesh DMesh;
    public Material DMaterial;
    [Range(1, 511)]
    public int Count = 100;

    private Matrix4x4[] models;
    private ComputeBuffer instanceBuffer;
    void Start()
    {
        UpdateInstance();
    }
    void UpdateInstance()
    {
        if (Count < 1) Count = 1;
        if (Count > 511) Count = 511;
        instanceBuffer?.Release();
        models = new Matrix4x4[Count];
        var paras = new InstancePara[Count];
        var parentPosition = transform.position;
        int row = Mathf.FloorToInt(Mathf.Sqrt(Count - 1)) + 1;
        for (int i = 0, k = 0; i < row; i++)
        {
            for (int j = 0; j < row && k < Count; j++, k++)
            {
                paras[k].position = parentPosition + new Vector3(0, i * 2, j * 2);
                models[0] = Matrix4x4.identity;
                paras[k].color = new Vector4(Random.Range(0.0f, 0.8f), Random.value, Random.value, 1.0f);
            }
        }
        instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
        instanceBuffer.SetData(paras);
        DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Count++;
            UpdateInstance();
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            Count--;
            UpdateInstance();
        }
        Graphics.DrawMeshInstanced(DMesh, 0, DMaterial, models, Count);
    }
    private void OnDisable()
    {
        instanceBuffer?.Release();
    }
    struct InstancePara
    {
        public Vector4 position;
        public Vector4 color;
        public const int SIZE = 4 * sizeof(float) + 4 * sizeof(float);
    }
}
