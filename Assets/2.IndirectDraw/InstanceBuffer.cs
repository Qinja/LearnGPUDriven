using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceBuffer : MonoBehaviour
{
    public Mesh DMesh;
    public Material DMaterial;
    [Range(1, 1023)]
    public int Count = 100;

    private Matrix4x4[] models;
    private ComputeBuffer buffer;
    void Start()
    {
        InitInstance();
    }
    void InitInstance()
    {
        if (Count < 1) Count = 1;
        if (Count > 1023) Count = 1023;
        if(buffer != null)
        {
            buffer.Release();
            buffer.Dispose();
            buffer = null;
        }
        models = new Matrix4x4[Count];
        models[0] = Matrix4x4.identity;
        var paras = new InstancePara[Count];
        var parentPosition = transform.position;
        int row = Mathf.CeilToInt(Mathf.Sqrt(Count));
        for (int i = 0, k = 0; i < row; i++)
        {
            for (int j = 0; j < row && k < Count; j++, k++)
            {
                paras[k].position = parentPosition + new Vector3(0, i * 2, j * 2);
                paras[k].color = new Vector4(Random.Range(0.0f, 0.8f), Random.value, Random.value, 1.0f);
            }
        }
        buffer = new ComputeBuffer(Count, InstancePara.SIZE);
        buffer.SetData(paras);
        DMaterial.SetBuffer("_InstanceBuffer", buffer);
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Count++;
            InitInstance();
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            Count--;
            InitInstance();
        }
        Graphics.DrawMeshInstanced(DMesh, 0, DMaterial, models, Count);
    }
    private void OnDisable()
    {
        buffer.Release();
        buffer.Dispose();
    }
    struct InstancePara
    {
        public Vector4 position;
        public Vector4 color;
        public const int SIZE = 4 * sizeof(float) + 4 * sizeof(float);
    }
}
