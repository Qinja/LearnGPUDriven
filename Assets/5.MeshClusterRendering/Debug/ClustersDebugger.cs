using UnityEngine;

public class ClustersDebugger : MonoBehaviour
{
    public ClustersData DCluster;
    public Material DMaterial;
    private Mesh[] meshes;
    public bool ShowAll = true;
    [Range(0.0f, 1.0f)]
    public float ShowIndex = 0.0f;
    void Start()
    {
        meshes = new Mesh[DCluster.Count];
        var ibo = new int[DCluster.QuadsPerCluster * 4];
        for(int i = 0; i < DCluster.QuadsPerCluster * 4; i++)
        {
            ibo[i] = i;
        }
        for (int i = 0; i < DCluster.Count; i++)
        {
            meshes[i] = new Mesh();
            meshes[i].name = DCluster.name;
            var vbo = new Vector3[DCluster.QuadsPerCluster * 4];
            for (int j = 0; j < DCluster.QuadsPerCluster * 4; j++)
            {
                vbo[j] = DCluster.Vertices[i * DCluster.QuadsPerCluster * 4 + j];
            }
            meshes[i].vertices = vbo;
            meshes[i].SetIndices(ibo, MeshTopology.Quads, 0);
            meshes[i].RecalculateNormals();
            meshes[i].RecalculateTangents();
            meshes[i].RecalculateBounds();
            meshes[i].UploadMeshData(false);
        }
    }

    void Update()
    {
        if(ShowAll)
        {
            for(int i = 0; i < DCluster.Count; i++)
            {
                Graphics.DrawMesh(meshes[i], transform.localToWorldMatrix, DMaterial, 0);
            }
        }
        else
        {
            int i = (int)(ShowIndex * (DCluster.Count - 1));
            Graphics.DrawMesh(meshes[i], transform.localToWorldMatrix, DMaterial, 0);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
            if (ShowAll)
            {
                for (int i = 0; i < DCluster.Count; i++)
                {
                    Gizmos.DrawWireCube(DCluster.BoundingBoxes[i].center, DCluster.BoundingBoxes[i].size);
                }
            }
            else
            {
                int i = (int)(ShowIndex * (DCluster.Count - 1));
                Gizmos.DrawWireCube(DCluster.BoundingBoxes[i].center, DCluster.BoundingBoxes[i].size);
            }
        }
    }
}