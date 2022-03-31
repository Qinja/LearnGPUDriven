using UnityEngine;

namespace MeshClusterRendering
{
	public class ClustersRendering : MonoBehaviour
	{
		public ClustersData DClusters;
		public Material DMaterial;
		public int Count;

		private const int CLUSTER_VERTEX_COUNT = 64;
		private Bounds proxyBounds;
		private ComputeBuffer instanceBuffer;
		private ComputeBuffer vertexBuffer;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			InitClusters();
			UpdateInstance();
		}
		private void InitClusters()
		{
			var vertexData = DClusters.Vertices;
			vertexBuffer = new ComputeBuffer(vertexData.Length, 3 * sizeof(float));
			vertexBuffer.SetData(vertexData);
			DMaterial.SetBuffer("_VertexBuffer", vertexBuffer);
			DMaterial.SetInteger("_ClusterCount", DClusters.ClusterCount);
		}
		private void UpdateInstance()
		{
			if (Count < 1) Count = 1;
			var instanceParas = new InstancePara[Count];
			var row = Mathf.FloorToInt(Mathf.Pow(Count - 1, 1.0f / 3.0f)) + 1;
			var parentPosition = transform.position;
			Random.InitState(0);
			for (int i = 0, n = 0; i < row; i++)
			{
				for (int j = 0; j < row; j++)
				{
					for (int k = 0; k < row && n < Count; k++, n++)
					{
						instanceParas[n].model = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i, j, k), Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.0f));
						instanceParas[n].color = new Vector4(Random.Range(0.7f, 1.0f), Random.Range(0.1f, 0.6f), Random.value, 1);
					}
				}
			}
			instanceBuffer?.Release();
			instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
			instanceBuffer.SetData(instanceParas);
			DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
		}
		void Update()
		{
			if (Input.GetKey(KeyCode.UpArrow))
			{
				Count = Count + Mathf.Max(Count / 10, 1);
				UpdateInstance();
			}
			else if (Input.GetKey(KeyCode.DownArrow))
			{
				Count = Count - Mathf.Max(Count / 10, 1);
				UpdateInstance();
			}
			Graphics.DrawProcedural(DMaterial, proxyBounds, MeshTopology.Quads, CLUSTER_VERTEX_COUNT, Count * DClusters.ClusterCount);
		}
		private void OnDisable()
		{
			instanceBuffer?.Release();
			vertexBuffer?.Release();
		}
		struct InstancePara
		{
			public Matrix4x4 model;
			public Vector4 color;
			public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
		}
	}
}