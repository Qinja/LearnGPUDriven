using UnityEngine;

namespace IndirectDraw
{
	public class IndirectInstanceBuffer : MonoBehaviour
	{
		public Mesh DMesh;
		public Material DMaterial;
		public int Count;

		private Bounds proxyBounds;
		private ComputeBuffer argsBuffer;
		private ComputeBuffer instanceBuffer;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 100.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			UpdateInstance();
		}
		void UpdateInstance()
		{
			if (Count < 1) Count = 1;
			instanceBuffer?.Release();
			var paras = new InstancePara[Count];
			var parentPosition = transform.position;
			int row = Mathf.FloorToInt(Mathf.Sqrt(Count - 1)) + 1;
			Random.InitState(0);
			for (int i = 0, k = 0; i < row; i++)
			{
				for (int j = 0; j < row && k < Count; j++, k++)
				{
					paras[k].model = Matrix4x4.TRS(parentPosition + new Vector3(0, i * 2, j * 2), Quaternion.identity, Vector3.one * Random.Range(0.2f, 1.2f));
					paras[k].color = new Vector4(Random.Range(0.0f, 0.8f), Random.value, Random.value, 1.0f);
				}
			}
			instanceBuffer = new ComputeBuffer(Count, InstancePara.SIZE);
			instanceBuffer.SetData(paras);
			DMaterial.SetBuffer("_InstanceBuffer", instanceBuffer);
			argsBuffer.SetData(new uint[5] { DMesh.GetIndexCount(0), (uint)Count, 0, 0, 0 });
		}
		void Update()
		{
			if (Input.GetKey(KeyCode.UpArrow))
			{
				Count++;
				UpdateInstance();
			}
			else if (Input.GetKey(KeyCode.DownArrow) && Count > 1)
			{
				Count--;
				UpdateInstance();
			}
			Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
		}
		private void OnDestroy()
		{
			argsBuffer?.Release();
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