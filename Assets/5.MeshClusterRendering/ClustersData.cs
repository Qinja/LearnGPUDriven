using UnityEngine;

public class ClustersData : ScriptableObject
{
    public Mesh RawMesh;
    public string Name;
    public int QuadsPerCluster;
    public Vector3[] Vertices;
    public Bounds[] BoundingBoxes;
}