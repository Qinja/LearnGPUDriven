using UnityEngine;

namespace OcclusionCulling
{
	public class PreZWriteUAV : MonoBehaviour
	{
		public Mesh DMesh;
		public Camera DCamera;
		public Material DMaterial;
		public Material DOcclusionMaterial;
		public int Count;

		private uint indexCount;
		private Bounds proxyBounds;
		private ComputeBuffer argsBuffer;
		private ComputeBuffer visibilityIndexBuffer;
		private ComputeBuffer visibilitySignBuffer;
		private ComputeBuffer instanceBuffer;
		private GraphicsBuffer occlusionCubeIndexBuffer;
		private int frameIndex;
		private uint[] visibilities;
		private bool occlusionVaild = false;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			indexCount = DMesh.GetIndexCount(0);
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
			visibilityIndexBuffer?.Release();
			visibilityIndexBuffer = new ComputeBuffer(Count, sizeof(uint));
			visibilitySignBuffer?.Release();
			visibilitySignBuffer = new ComputeBuffer(Count, sizeof(uint));
			visibilitySignBuffer.SetData(new uint[Count]);
			Graphics.SetRandomWriteTarget(1, visibilitySignBuffer);

			frameIndex = Mathf.Max(frameIndex, Count);
			visibilities = new uint[Count];
			occlusionVaild = false;
			DOcclusionMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			DOcclusionMaterial.SetBuffer("_VisibilityFrameBuffer", visibilitySignBuffer);
			DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			DMaterial.SetBuffer("_VisibilityBuffer", visibilityIndexBuffer);
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

			uint visibleCount = 0;
			if (occlusionVaild)
			{
				//prev frame
				visibilitySignBuffer.GetData(visibilities);
				for (uint i = 0; i < Count; i++)
				{
					if (visibilities[i] == frameIndex - 1)
					{
						visibilities[visibleCount++] = i;
					}
				}
			}
			else
			{
				for (; visibleCount < Count; visibleCount++)
				{
					visibilities[visibleCount] = visibleCount;
				}
				occlusionVaild = true;
			}

			//current frame
			DOcclusionMaterial.SetInt("_CurrentFrameIndex", frameIndex);
			Graphics.DrawProcedural(DOcclusionMaterial, proxyBounds, MeshTopology.Triangles, occlusionCubeIndexBuffer, 36, Count, DCamera);
			visibilityIndexBuffer.SetData(visibilities);
			argsBuffer.SetData(new uint[5] { indexCount, visibleCount, 0, 0, 0 });
			Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
			frameIndex++;
		}
		private void OnDisable()
		{
			visibilityIndexBuffer?.Release();
			instanceBuffer?.Release();
			visibilitySignBuffer?.Release();
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