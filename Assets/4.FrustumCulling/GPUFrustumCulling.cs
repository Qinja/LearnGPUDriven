using UnityEngine;

namespace FrustumCulling
{
	public class GPUFrustumCulling : MonoBehaviour
	{
		public Camera DCamera;
		public Mesh DMesh;
		public Material DMaterial;
		public int Count;
		public ComputeShader DComputeShader;

		private const int KERNEL_SIZE_X = 64;
		private uint indexCount;
		private int cullKernelID;
		private int kernelGroupX;
		private Bounds proxyBounds;
		private ComputeBuffer argsBuffer;
		private ComputeBuffer visibilityBuffer;
		private ComputeBuffer instanceBuffer;
		private Plane[] cullingPlanes = new Plane[6];
		private Vector4[] cullingPlaneVectors = new Vector4[6];
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			cullKernelID = DComputeShader.FindKernel("FrustumCullMain");
			indexCount = DMesh.GetIndexCount(0);
			var bounds = DMesh.bounds;
			DComputeShader.SetVector("_BoundsCenter", bounds.center);
			DComputeShader.SetVector("_BoundsExtent", new Vector3(0.5f, 0.5f, 0.5f));
			DComputeShader.SetBuffer(cullKernelID, "_ArgsBuffer", argsBuffer);
			UpdateInstance();
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
			visibilityBuffer?.Release();
			visibilityBuffer = new ComputeBuffer(Count, sizeof(uint));
			visibilityBuffer.SetData(new uint[Count]);

			DComputeShader.SetBuffer(cullKernelID, "_InstanceBuffer", instanceBuffer);
			DComputeShader.SetBuffer(cullKernelID, "_VisibilityBuffer", visibilityBuffer);
			DComputeShader.SetInt("_Count", Count);
			DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			DMaterial.SetBuffer("_VisibilityBuffer", visibilityBuffer);
			kernelGroupX = Mathf.CeilToInt(1.0f * Count / KERNEL_SIZE_X);
		}
		private void UpdateCullingPlane()
		{
			GeometryUtility.CalculateFrustumPlanes(DCamera, cullingPlanes);
			for(int i = 0; i < 6; i++)
			{
				var normal = cullingPlanes[i].normal;
				cullingPlaneVectors[i] = new Vector4(normal.x, normal.y, normal.z, cullingPlanes[i].distance);
			}
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
			DComputeShader.SetVectorArray("_CullingPlanes", cullingPlaneVectors);
			argsBuffer.SetData(new uint[5] { indexCount, 0, 0, 0, 0 });
			DComputeShader.Dispatch(cullKernelID, kernelGroupX, 1, 1);
			Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
		}
		private void OnDestroy()
		{
			argsBuffer?.Release();
			visibilityBuffer?.Release();
			instanceBuffer?.Release();
		}
		struct InstancePara
		{
			public Matrix4x4 model;
			public Vector4 color;
			public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
		}
	}
}