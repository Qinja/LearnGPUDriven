using System.Collections.Generic;
using UnityEngine;

namespace MeshClusterRendering
{
	public class MultiMeshClusters : MonoBehaviour
	{
		public Camera DCamera;
		public List<ClustersData> DClusters;
		public Material DMaterial;
		public int Count;
		public ComputeShader DComputeShader;

		private const int CLUSTER_VERTEX_COUNT = 64;
		private const int KERNEL_SIZE_X = 64;
		private int cullKernelID;
		private int kernelGroupX;
		private List<uint> vertexOffset;
		private Bounds proxyBounds;
		private GraphicsBuffer indexBuffer;
		private ComputeBuffer argsBuffer;
		private ComputeBuffer clustersBuffer;
		private ComputeBuffer visibilityBuffer;
		private ComputeBuffer instanceBuffer;
		private ComputeBuffer vertexBuffer;
		private ComputeBuffer boundsBuffer;
		private Plane[] cullingPlanes = new Plane[6];
		private Vector4[] cullingPlaneVectors = new Vector4[6];
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			cullKernelID = DComputeShader.FindKernel("FrustumCullMain");
			DComputeShader.SetBuffer(cullKernelID, "_ArgsBuffer", argsBuffer);
			InitClusters();
			InitIndexBuffer();
			UpdateInstance();
		}
		private void InitIndexBuffer()
		{
			var indexLength = CLUSTER_VERTEX_COUNT / 4 * 6;
			var indexData = new ushort[indexLength];
			for (int i = 0; i < CLUSTER_VERTEX_COUNT / 4; i++)
			{
				indexData[i * 6 + 0] = (ushort)(i * 4 + 0);
				indexData[i * 6 + 1] = (ushort)(i * 4 + 1);
				indexData[i * 6 + 2] = (ushort)(i * 4 + 2);
				indexData[i * 6 + 3] = (ushort)(i * 4 + 0);
				indexData[i * 6 + 4] = (ushort)(i * 4 + 2);
				indexData[i * 6 + 5] = (ushort)(i * 4 + 3);
			}
			indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexLength, sizeof(ushort));
			indexBuffer.SetData(indexData);
		}
		private void InitClusters()
		{
			vertexOffset = new List<uint>();
			var vertexCount = 0u;
			var vertexData = new List<Vector3>();
			var boxes = new List<Bounds>();
			foreach (var clusters in DClusters)
			{
				vertexOffset.Add(vertexCount);
				vertexCount += (uint)clusters.Vertices.Length;
				vertexData.AddRange(clusters.Vertices);
				boxes.AddRange(clusters.BoundingBoxes);
			}
			vertexBuffer = new ComputeBuffer(vertexData.Count, 3 * sizeof(float));
			vertexBuffer.SetData(vertexData);
			DMaterial.SetBuffer("_VertexBuffer", vertexBuffer);
			boundsBuffer = new ComputeBuffer(boxes.Count, 2 * 3 * sizeof(float));
			boundsBuffer.SetData(boxes);
			DComputeShader.SetBuffer(cullKernelID, "_BoundingBoxes", boundsBuffer);
		}
		private void UpdateInstance()
		{
			if (Count < 1) Count = 1;
			var instanceParas = new InstancePara[Count];
			var row = Mathf.FloorToInt(Mathf.Pow(Count - 1, 1.0f / 3.0f)) + 1;
			var parentPosition = transform.position;
			Random.InitState(0);
			var clusterCount = 0;
			var clusterData = new List<uint>();
			for (uint i = 0, n = 0; i < row; i++)
			{
				for (int j = 0; j < row; j++)
				{
					for (int k = 0; k < row && n < Count; k++, n++)
					{
						var meshId = Random.Range(0, DClusters.Count);
						var meshPosition = parentPosition + (20.0f + 2.0f * row) * (meshId - 0.5f * DClusters.Count) * Vector3.right + 10.0f * Vector3.right;
						var meshColorBMin = 1.0f * meshId / DClusters.Count;
						var meshColorBMax = 1.0f * (meshId + 1) / DClusters.Count;
						var count = DClusters[meshId].Count;
						instanceParas[n].model = Matrix4x4.TRS(meshPosition + 2.0f * new Vector3(i, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
						instanceParas[n].color = new Vector3(Random.Range(0.2f, 0.8f), Random.Range(0.5f, 1.0f), Random.Range(meshColorBMin, meshColorBMax));
						instanceParas[n].vertexOffset = vertexOffset[meshId];
						instanceParas[n].clusterOffset = (uint)clusterCount;
						clusterCount += count;
						for (int l = 0; l < count; l++) clusterData.Add(n);
					}
				}
			}
			instanceBuffer?.Release();
			visibilityBuffer?.Release();
			clustersBuffer?.Release();
			instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
			visibilityBuffer = new ComputeBuffer(clusterCount, sizeof(uint));
			clustersBuffer = new ComputeBuffer(clusterCount, sizeof(uint));
			instanceBuffer.SetData(instanceParas);
			visibilityBuffer.SetData(new uint[clusterCount]);
			clustersBuffer.SetData(clusterData);

			DComputeShader.SetBuffer(cullKernelID, "_InstanceBuffer", instanceBuffer);
			DComputeShader.SetBuffer(cullKernelID, "_VisibilityBuffer", visibilityBuffer);
			DComputeShader.SetBuffer(cullKernelID, "_ClusterBuffer", clustersBuffer);

			DComputeShader.SetInt("_ClusterCount", clusterCount);
			DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			DMaterial.SetBuffer("_VisibilityBuffer", visibilityBuffer);
			DMaterial.SetBuffer("_ClusterBuffer", clustersBuffer);
			kernelGroupX = Mathf.CeilToInt(1.0f * clusterCount / KERNEL_SIZE_X);
		}
		private void UpdateCullingPlane()
		{
			GeometryUtility.CalculateFrustumPlanes(DCamera, cullingPlanes);
			for (int i = 0; i < 6; i++)
			{
				var normal = cullingPlanes[i].normal;
				cullingPlaneVectors[i] = new Vector4(normal.x, normal.y, normal.z, cullingPlanes[i].distance);
			}
			DComputeShader.SetVectorArray("_CullingPlanes", cullingPlaneVectors);
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
			UpdateCullingPlane();
			argsBuffer.SetData(new uint[5] { (uint)indexBuffer.count, 0, 0, 0, 0 });
			DComputeShader.Dispatch(cullKernelID, kernelGroupX, 1, 1);
			Graphics.DrawProceduralIndirect(DMaterial, proxyBounds, MeshTopology.Triangles, indexBuffer, argsBuffer);
		}
		private void OnDisable()
		{
			clustersBuffer?.Release();
			indexBuffer?.Release();
			argsBuffer?.Release();
			visibilityBuffer?.Release();
			instanceBuffer?.Release();
			vertexBuffer?.Release();
			boundsBuffer?.Release();
		}
		struct InstancePara
		{
			public Matrix4x4 model;
			public Vector4 color;
			public uint vertexOffset;
			public uint clusterOffset;
			public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float) + 2 * sizeof(int);
		}
	}
}