using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ClustersDataGenerator
{
    [MenuItem("Tools/SaveSelectionMesh")]
    static void SaveSelectionMesh()
    {
        var gameObj = Selection.activeGameObject;
        var mesh = gameObj?.GetComponent<MeshFilter>()?.sharedMesh;
        if(mesh != null)
        {
            var path = EditorUtility.SaveFilePanelInProject("Save Mesh Asset", "RawMesh.asset", "asset", "Please enter a file name to save the mesh data to");
            if(!string.IsNullOrEmpty(path))
            {
                var mesh2 = Object.Instantiate(mesh);
                AssetDatabase.CreateAsset(mesh2, path);
            }
        }
    }
    [MenuItem("Tools/SplitSelectionMesh")]
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
                AssetDatabase.CreateAsset(clusters, path);
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

    const int QUAD_COUNT = 16;
    struct Quad
    {
        public Vector3 vt1;
        public Vector3 vt2;
        public Vector3 vt3;
        public Vector3 vt4;
    }
    static ClustersData QuadsToClusters(List<Vector3> inQuads)
    {
        var quadLists = new List<List<Quad>>();
        var boundsList = new List<Bounds>();
        for (int i = 0; i < inQuads.Count / 4; i++)
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
            for (int i = 0; i < quadLists.Count; i++)
            {
                if (quadLists[i].Count >= QUAD_COUNT || quadLists[i].Count == 0) continue;
                for (int j = i + 1; j < quadLists.Count; j++)
                {
                    if (quadLists[j].Count >= QUAD_COUNT || quadLists[j].Count == 0) continue;
                    if (quadLists[j].Count + quadLists[i].Count >= QUAD_COUNT) continue;
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
        quadLists.ForEach(list =>
        {
            var add = QUAD_COUNT - list.Count;
            var lastVertex = list[list.Count - 1].vt4;
            for (int j = 0; j < add; j++)
            {
                list.Add(new Quad()
                {
                    vt1 = lastVertex,
                    vt2 = lastVertex,
                    vt3 = lastVertex,
                    vt4 = lastVertex
                });
            }
        });
        var clusters = new Vector3[quadLists.Count * QUAD_COUNT * 4];
        var boxes = new Bounds[quadLists.Count];
        for (int i = 0, n = 0; i < quadLists.Count; i++)
        {
            boxes[i] = new Bounds(quadLists[i][0].vt1, Vector3.zero);
            foreach (var quadList in quadLists[i])
            {
                clusters[n++] = quadList.vt1;
                clusters[n++] = quadList.vt2;
                clusters[n++] = quadList.vt3;
                clusters[n++] = quadList.vt4;
                if(quadList.vt1 != quadList.vt2 && quadList.vt1 != quadList.vt3 && quadList.vt1 != quadList.vt4)
                {
                    boxes[i].Encapsulate(quadList.vt1);
                    boxes[i].Encapsulate(quadList.vt2);
                    boxes[i].Encapsulate(quadList.vt3);
                    boxes[i].Encapsulate(quadList.vt4);
                }
            }
        }
        ClustersData outData = ScriptableObject.CreateInstance<ClustersData>();
        outData.Vertices = clusters;
        outData.QuadsPerCluster = QUAD_COUNT;
        outData.BoundingBoxes = boxes;
        outData.Count = quadLists.Count;
        return outData;
    }
}
