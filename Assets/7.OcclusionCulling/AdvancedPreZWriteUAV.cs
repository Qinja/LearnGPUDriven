using UnityEngine;
using UnityEngine.Rendering;

namespace OcclusionCulling
{
	public class AdvancedPreZWriteUAV : MonoBehaviour
	{
		public Mesh DMesh;
		public Camera DCamera;
		public Material DMaterial;
		public Material DOcclusionMaterial;
		public int Count;

		private Bounds proxyBounds;
		private ComputeBuffer argsBuffer;
		private ComputeBuffer visibilityBuffer;
		private ComputeBuffer visibilityFrameIndexBuffer;
		private ComputeBuffer instanceBuffer;
		private GraphicsBuffer occlusionCubeIndexBuffer;
		private int frameIndex;
		private uint[] emptyArgsData;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			emptyArgsData = new uint[5] { DMesh.GetIndexCount(0), 0, 0, 0, 0 };
			var indexData = new ushort[36] {
				1,0,3, 1,3,2,
				0,1,5, 0,5,4,
				0,4,7, 0,7,3,
				5,1,2, 5,2,6,
				7,6,2, 7,2,3,
				4,5,6, 4,6,7,
			};
			occlusionCubeIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexData.Length, sizeof(ushort));
			occlusionCubeIndexBuffer.SetData(indexData);
			DOcclusionMaterial.SetVector("_BoundsExtent", DMesh.bounds.extents);
			DOcclusionMaterial.SetVector("_BoundsCenter", DMesh.bounds.center);
			UpdateInstance();
		}
		private void UpdateCommandBuffer()
		{
			DCamera.RemoveAllCommandBuffers();
			var commandBuffer = new CommandBuffer();
			commandBuffer.name = nameof(AdvancedPreZWriteUAV);
			commandBuffer.SetRandomWriteTarget(1, visibilityFrameIndexBuffer);
			commandBuffer.SetRandomWriteTarget(2, visibilityBuffer);
			commandBuffer.SetRandomWriteTarget(3, argsBuffer);
			commandBuffer.SetBufferData(argsBuffer, emptyArgsData);
			commandBuffer.DrawProcedural(occlusionCubeIndexBuffer, Matrix4x4.identity, DOcclusionMaterial, 0, MeshTopology.Triangles, 36, Count);
			commandBuffer.ClearRandomWriteTargets();
			//commandBuffer.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, 0, argsBuffer);
			DCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
		}
		private void UpdateInstance()
		{
			if (Count < 1) Count = 1;
			frameIndex = Mathf.Max(frameIndex, Count);
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
						instanceParas[n].model = Matrix4x4.TRS(parentPosition + 2.0f * new Vector3(i, j, k), Random.rotationUniform, Vector3.one * Random.Range(0.5f, 1.0f));
						instanceParas[n].color = new Vector4(Random.Range(0.7f, 1.0f), Random.Range(0.1f, 0.6f), Random.value, 1);
					}
				}
			}
			Graphics.ClearRandomWriteTargets();
			instanceBuffer?.Release();
			instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
			instanceBuffer.SetData(instanceParas);
			visibilityBuffer?.Release();
			visibilityBuffer = new ComputeBuffer(Count, sizeof(uint));
			visibilityFrameIndexBuffer?.Release();
			visibilityFrameIndexBuffer = new ComputeBuffer(Count, sizeof(uint));
			visibilityFrameIndexBuffer.SetData(new uint[Count]);

			DOcclusionMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			DOcclusionMaterial.SetBuffer("_VisibilityFrameIndexBuffer", visibilityFrameIndexBuffer);
			DOcclusionMaterial.SetBuffer("_VisibilityBuffer", visibilityBuffer);
			DOcclusionMaterial.SetBuffer("_ArgsBuffer", argsBuffer);
			DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			DMaterial.SetBuffer("_VisibilityBuffer", visibilityBuffer);
			UpdateCommandBuffer();
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

			//current frame
			DOcclusionMaterial.SetInt("_CurrentFrameIndex", frameIndex);
			frameIndex++;
			Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
		}
		private void OnDisable()
		{
			visibilityBuffer?.Release();
			instanceBuffer?.Release();
			visibilityFrameIndexBuffer?.Release();
			argsBuffer?.Release();
			occlusionCubeIndexBuffer?.Release();
		}
		struct InstancePara
		{
			public Matrix4x4 model;
			public Vector4 color;
			public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
		}
	}
}