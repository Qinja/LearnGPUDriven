using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshInstanced : MonoBehaviour
{
    public Mesh DMesh;
    public Material DMaterial;
    private Matrix4x4[] models;
    void Start()
    {
        models = new Matrix4x4[25];
        var parentPosition = transform.position;
        for(int i = 0, k = 0; i < 5; i++)
        {
            for(int j = 0; j < 5; j++, k++)
            {
                var pos = parentPosition + new Vector3(0, i * 2, j * 2);
                models[k] = Matrix4x4.Translate(pos);
            }
        }
    }
    void Update()
    {
        Graphics.DrawMeshInstanced(DMesh, 0, DMaterial, models);       
    }
}
