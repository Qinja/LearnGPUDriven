using UnityEngine;

namespace BackFaceCulling
{
	public class ClustersData : ScriptableObject
	{
		public Mesh RawMesh;
		public string Name;
		public int QuadsPerCluster;
		public int ClusterCount;
		public Vector3[] Vertices;
		public ClusterBoundsData[] ClusterBounds;
		[System.Serializable]
		public struct ClusterBoundsData
		{
			public BoundingSphere BoundingSphere;
			public BoundingCone BoundingCone;
			public const int SIZE = BoundingSphere.SIZE + BoundingCone.SIZE;
		}
	}
}