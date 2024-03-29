using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BackFaceCulling
{
    public class ClustersDataGenerator
    {
        [MenuItem("Tools/6.SplitSelectionMesh", false, 6)]
        static void SplitSelectionMesh()
        {
            var mesh = Selection.activeObject as Mesh;
            if (mesh != null)
            {
                var quads = MeshToQuads(mesh);
                var clusters = QuadsToClusters(quads);
                clusters.RawMesh = mesh;
                clusters.Name = mesh.name + "_Clusters";
                var path = EditorUtility.SaveFilePanelInProject("Save Clusters Asset", "Clusters.asset", "asset", "Please enter a file name to save the clusters data to");
                if (!string.IsNullOrEmpty(path))
                {
                    var oldData = AssetDatabase.LoadAssetAtPath<ClustersData>(path);
                    if (oldData != null)
                    {
                        oldData.RawMesh = clusters.RawMesh;
                        oldData.Name = clusters.Name;
                        oldData.QuadsPerCluster = clusters.QuadsPerCluster;
                        oldData.ClusterCount = clusters.ClusterCount;
                        oldData.Vertices = clusters.Vertices;
                        oldData.ClusterBounds = clusters.ClusterBounds;
                        EditorUtility.SetDirty(oldData);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(clusters, path);
                    }
                }
            }
        }

        struct Triangle
        {
            public int v1;
            public int v2;
            public int v3;
            public bool merged;
        }
        static List<Vector3> MeshToQuads(Mesh mesh)
        {
            var vts = mesh.vertices;
            var trisInt = mesh.triangles;
            Triangle[] tris = new Triangle[trisInt.Length / 3];
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = new Triangle() { v1 = trisInt[i * 3], v2 = trisInt[i * 3 + 1], v3 = trisInt[i * 3 + 2] };
            }
            var vtsQuad = new List<Vector3>();
            for (int i = 0; i < tris.Length; i++)
            {
                var triSource = tris[i];
                if (triSource.merged) continue;
                bool foundTarget = false;
                for (int j = i + 1; j < tris.Length; j++)
                {
                    var triTarget = tris[j];
                    if (triTarget.merged) continue;
                    List<int> quadIndexList = new List<int>();
                    quadIndexList.Add(triSource.v1);
                    quadIndexList.Add(triSource.v2);
                    quadIndexList.Add(triSource.v3);
                    bool[] repeatIndices = new bool[3];
                    int repeatIndex = quadIndexList.IndexOf(triTarget.v1);
                    if (repeatIndex != -1) repeatIndices[repeatIndex] = true;
                    else quadIndexList.Add(triTarget.v1);
                    repeatIndex = quadIndexList.IndexOf(triTarget.v2);
                    if (repeatIndex != -1) repeatIndices[repeatIndex] = true;
                    else quadIndexList.Add(triTarget.v2);
                    repeatIndex = quadIndexList.IndexOf(triTarget.v3);
                    if (repeatIndex != -1) repeatIndices[repeatIndex] = true;
                    else quadIndexList.Add(triTarget.v3);
                    if (quadIndexList.Count == 4)
                    {
                        tris[i].merged = true;
                        tris[j].merged = true;
                        vtsQuad.Add(vts[quadIndexList[0]]);
                        if (!repeatIndices[2]) vtsQuad.Add(vts[quadIndexList[3]]);//triSource v1 v2 common used
                        vtsQuad.Add(vts[quadIndexList[1]]);
                        if (!repeatIndices[0]) vtsQuad.Add(vts[quadIndexList[3]]);//triSource v2 v3 common used
                        vtsQuad.Add(vts[quadIndexList[2]]);
                        if (!repeatIndices[1]) vtsQuad.Add(vts[quadIndexList[3]]);//triSource v1 v3 common used
                        foundTarget = true;
                        break;
                    }
                }
                if (!foundTarget)
                {
                    tris[i].merged = true;
                    vtsQuad.Add(vts[triSource.v1]);
                    vtsQuad.Add(vts[triSource.v2]);
                    vtsQuad.Add(vts[triSource.v3]);
                    vtsQuad.Add(vts[triSource.v1]);
                }
            }
            return vtsQuad;
        }

        const int QUAD_PER_CLUSTER = 16;
        struct Quad
        {
            public Vector3 vt1;
            public Vector3 vt2;
            public Vector3 vt3;
            public Vector3 vt4;
        }
        static ClustersData QuadsToClusters(List<Vector3> inQuads)
        {
            var quadCount = inQuads.Count / 4;
            var quadLists = new List<List<Quad>>(quadCount);
            var boundsList = new List<Bounds>(quadCount);
            for (int i = 0; i < quadCount; i++)
            {
                var quadList = new List<Quad>()
                {
                    new Quad() { vt1 = inQuads[i * 4], vt2 = inQuads[i * 4 + 1], vt3 = inQuads[i * 4 + 2], vt4 = inQuads[i * 4 + 3]}
                };
                quadLists.Add(quadList);
                var bounds = new Bounds(quadList[0].vt1, Vector3.zero);
                bounds.Encapsulate(quadList[0].vt2);
                bounds.Encapsulate(quadList[0].vt3);
                bounds.Encapsulate(quadList[0].vt4);
                boundsList.Add(bounds);
            }
            while (true)
            {
                float minVolume = float.MaxValue;
                int mergeI = -1;
                int mergeJ = -1;
                for (int i = 0; i < quadCount; i++)
                {
                    if (quadLists[i].Count >= QUAD_PER_CLUSTER || quadLists[i].Count == 0) continue;
                    for (int j = i + 1; j < quadCount; j++)
                    {
                        if (quadLists[j].Count >= QUAD_PER_CLUSTER || quadLists[j].Count == 0) continue;
                        if (quadLists[j].Count + quadLists[i].Count >= QUAD_PER_CLUSTER) continue;
                        var bounds = boundsList[i];
                        bounds.Encapsulate(boundsList[j]);
                        if (bounds.extents.sqrMagnitude < minVolume)
                        {
                            minVolume = bounds.extents.sqrMagnitude;
                            mergeI = i;
                            mergeJ = j;
                        }
                    }
                }
                if (mergeI >= 0 && mergeJ >= 0)
                {
                    quadLists[mergeI].AddRange(quadLists[mergeJ]);
                    quadLists[mergeJ].Clear();
                    var bounds = boundsList[mergeI];
                    bounds.Encapsulate(boundsList[mergeJ]);
                    boundsList[mergeI] = bounds;
                }
                else
                {
                    break;
                }
            }
            quadLists.RemoveAll(list => list.Count == 0);
            var clusters = new Vector3[quadLists.Count * QUAD_PER_CLUSTER * 4];
            var boundsData = new ClustersData.ClusterBoundsData[quadLists.Count];
            var rAvg = 0f;
            for (int i = 0, n = 0; i < quadLists.Count; i++)
            {
                var list = quadLists[i];
                boundsData[i] = new ClustersData.ClusterBoundsData();
                var normals = new List<Vector3>();
                var positions = new List<Vector3>();
                foreach (var quad in list)
                {
                    clusters[n++] = quad.vt1;
                    clusters[n++] = quad.vt2;
                    clusters[n++] = quad.vt3;
                    clusters[n++] = quad.vt4;
                    positions.Add(quad.vt1);
                    positions.Add(quad.vt2);
                    positions.Add(quad.vt3);
                    positions.Add(quad.vt4);
                    var n1 = Vector3.Cross(quad.vt1 - quad.vt2, quad.vt1 - quad.vt3);
                    if (n1 != Vector3.zero) normals.Add(n1.normalized);
                    var n2 = Vector3.Cross(quad.vt1 - quad.vt3, quad.vt1 - quad.vt4);
                    if (n2 != Vector3.zero) normals.Add(n2.normalized);
                }
                boundsData[i].BoundingSphere = new BoundingSphere(positions.ToArray());
                boundsData[i].BoundingCone = new BoundingCone(normals.ToArray());
                if (boundsData[i].BoundingCone.cosAngle < 0)
                {
                    boundsData[i].BoundingCone.cosAngle = 0;
                    boundsData[i].BoundingCone.sinAngle = 1;
                }
                var lastVertex = list[list.Count - 1].vt4;
                for (int j = 0; j < QUAD_PER_CLUSTER - list.Count; j++)
                {
                    clusters[n++] = lastVertex;
                    clusters[n++] = lastVertex;
                    clusters[n++] = lastVertex;
                    clusters[n++] = lastVertex;
                }
                rAvg += boundsData[i].BoundingSphere.radius;
            }
            Debug.Log(rAvg / quadLists.Count);
            ClustersData outData = ScriptableObject.CreateInstance<ClustersData>();
            outData.Vertices = clusters;
            outData.QuadsPerCluster = QUAD_PER_CLUSTER;
            outData.ClusterBounds = boundsData;
            outData.ClusterCount = quadLists.Count;
            return outData;
        }
    }
}