using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegacyMultiMesh : MonoBehaviour
{
    public Mesh DMeshA;
    public Mesh DMeshB;
    public Material DMaterial;

    private Material DMaterialA;
    private Material DMaterialB;
    private Matrix4x4[] modelsA;
    private Matrix4x4[] modelsB;
    private void Start()
    {
        modelsA = new Matrix4x4[125];
        modelsB = new Matrix4x4[125];
        Vector4[] colorsA = new Vector4[125];
        Vector4[] colorsB = new Vector4[125];
        var parentPosition = transform.position;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 5; k++)
                {
                    modelsA[25 * i + 5 * j + k] = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i - 5, j, k), Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.0f));
                    modelsB[25 * i + 5 * j + k] = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i + 5, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
                    colorsA[25 * i + 5 * j + k] = new Vector4(Random.Range(0.0f, 0.7f), Random.Range(0.2f, 1.0f), Random.Range(0.3f, 1.0f), 1);
                    colorsB[25 * i + 5 * j + k] = new Vector4(Random.Range(0.7f, 1.0f), Random.Range(0.3f, 0.8f), Random.Range(0.0f, 0.4f), 1);
                }
            }
        }
        DMaterialA = Instantiate(DMaterial);
        DMaterialB = Instantiate(DMaterial);
        DMaterialA.SetVectorArray("_Color", colorsA);
        DMaterialB.SetVectorArray("_Color", colorsB);
    }
    void Update()
    {
        Graphics.DrawMeshInstanced(DMeshA, 0, DMaterialA, modelsA);
        Graphics.DrawMeshInstanced(DMeshB, 0, DMaterialB, modelsB);
    }
}
