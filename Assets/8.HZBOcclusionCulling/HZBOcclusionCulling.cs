using UnityEngine;
using UnityEngine.Rendering;

namespace HZBOcclusionCulling
{
	public class HZBOcclusionCulling : MonoBehaviour
	{
		public Mesh DMesh;
		public Camera DCamera;
		public HiZGenerator DHiZGenerator;
		public Material DMaterial;
		public Material DMaterialHZBOcclusion;
		public int Count;

		private Bounds proxyBounds;
		private ComputeBuffer argsBuffer;
		private ComputeBuffer visibilityBuffer;
		private ComputeBuffer visibilityFrameIndexBuffer;
		private ComputeBuffer instanceBuffer;
		private CommandBuffer hzbCommandBuffer;
		private int frameIndex;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			DMaterialHZBOcclusion.SetVector("_BoundsExtent", DMesh.bounds.extents);
			DMaterialHZBOcclusion.SetVector("_BoundsCenter", DMesh.bounds.center);
			DHiZGenerator.HiZBufferUpdated += UpdateHiZBuffer;
			InitCommandBuffer();
			UpdateInstance();
			UpdateHiZBuffer();
		}
		private void UpdateHiZBuffer()
		{
			DMaterialHZBOcclusion.SetTexture("_HiZBuffer", DHiZGenerator.HiZBuffer);
		}
		private void InitCommandBuffer()
		{
			hzbCommandBuffer = new CommandBuffer();
			hzbCommandBuffer.name = gameObject.name;
			DCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, hzbCommandBuffer);
		}
		private void UpdateCommandBuffer()
		{
			hzbCommandBuffer.Clear();
			hzbCommandBuffer.SetRandomWriteTarget(1, visibilityFrameIndexBuffer);
			hzbCommandBuffer.SetRandomWriteTarget(2, visibilityBuffer);
			hzbCommandBuffer.SetRandomWriteTarget(3, argsBuffer);
			hzbCommandBuffer.SetBufferData(argsBuffer, new uint[5] { DMesh.GetIndexCount(0), 0, 0, 0, 0 });
			hzbCommandBuffer.DrawProcedural(Matrix4x4.identity, DMaterialHZBOcclusion, 0, MeshTopology.Points, Count);
			hzbCommandBuffer.ClearRandomWriteTargets();
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

			DMaterialHZBOcclusion.SetBuffer("_InstanceBuffer", instanceBuffer);
			DMaterialHZBOcclusion.SetBuffer("_VisibilityFrameIndexBuffer", visibilityFrameIndexBuffer);
			DMaterialHZBOcclusion.SetBuffer("_VisibilityBuffer", visibilityBuffer);
			DMaterialHZBOcclusion.SetBuffer("_ArgsBuffer", argsBuffer);
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
			DMaterialHZBOcclusion.SetInt("_CurrentFrameIndex", frameIndex);
			Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
			frameIndex++;
		}
		private void OnDestroy()
		{
			visibilityBuffer?.Release();
			instanceBuffer?.Release();
			visibilityFrameIndexBuffer?.Release();
			argsBuffer?.Release();
		}
		struct InstancePara
		{
			public Matrix4x4 model;
			public Vector4 color;
			public const int SIZE = 16 * sizeof(float) + 4 * sizeof(float);
		}
	}
}