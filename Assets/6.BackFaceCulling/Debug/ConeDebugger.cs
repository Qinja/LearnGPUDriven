using UnityEngine;

namespace BackFaceCulling
{
    public class ConeDebugger : MonoBehaviour
    {
        public ClustersData DCluster;
        public Camera DCamera;
        public Material DMaterial;
        public bool ShowAll = true;
        [Range(0.0f, 1.0f)]
        public float ShowIndex = 0.0f;
        private Mesh[] meshes;
        void Start()
        {
            meshes = new Mesh[DCluster.ClusterCount];
            var ibo = new int[DCluster.QuadsPerCluster * 4];
            for (int i = 0; i < DCluster.QuadsPerCluster * 4; i++)
            {
                ibo[i] = i;
            }
            for (int i = 0; i < DCluster.ClusterCount; i++)
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
        void LateUpdate()
        {
            if (ShowAll)
            {
                for (int i = 0; i < DCluster.ClusterCount; i++)
                {
                    Graphics.DrawMesh(meshes[i], transform.localToWorldMatrix, DMaterial, 0);
                }
            }
            else
            {
                int i = (int)(ShowIndex * (DCluster.ClusterCount - 1));
                Graphics.DrawMesh(meshes[i], transform.localToWorldMatrix, DMaterial, 0);
            }
        }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if (ShowAll)
                {
                    for (int i = 0; i < DCluster.ClusterCount; i++)
                    {
                        GizemoDrawCone(DCluster.ClusterBounds[i].BoundingSphere.center, DCluster.ClusterBounds[i].BoundingCone, Color.yellow);
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(DCluster.ClusterBounds[i].BoundingSphere.center, DCluster.ClusterBounds[i].BoundingSphere.radius);
                    }
                }
                else
                {
                    int i = (int)(ShowIndex * (DCluster.ClusterCount - 1));
                    GizemoDrawCone(DCluster.ClusterBounds[i].BoundingSphere.center, DCluster.ClusterBounds[i].BoundingCone, Color.yellow);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(DCluster.ClusterBounds[i].BoundingSphere.center, DCluster.ClusterBounds[i].BoundingSphere.radius);
                }
            }
        }

        private void GizemoDrawCone(Vector3 position, BoundingCone cone, Color color)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, cone.normal);
            Gizmos.color = color;
            for (int i = 0; i < 100; i++)
            {
                var newDir = Random.onUnitSphere;
                Vector3 y = Vector3.Cross(newDir, cone.normal).normalized;
                Quaternion rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * Mathf.Acos(cone.cosAngle), y);
                var far = rotation * cone.normal;
                Gizmos.DrawRay(position, far);
            }
        }
    }
}