using UnityEngine;

namespace IndirectDraw
{
	public class IndirectInstance : MonoBehaviour
	{
		public Mesh DMesh;
		public Material DMaterial;
		public int Count;

		private Bounds proxyBounds;
		private ComputeBuffer argsBuffer;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 100.0f * Vector3.one);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			UpdateInstance();
		}
		void UpdateInstance()
		{
			if (Count < 1) Count = 1;
			argsBuffer.SetData(new uint[5] { DMesh.GetIndexCount(0), (uint)Count, 0, 0, 0 });
			DMaterial.SetInt("_InstanceCount", Count);
			DMaterial.SetVector("_ParentPosition", transform.position);
		}
		void Update()
		{
			if (Input.GetKey(KeyCode.UpArrow))
			{
				Count++;
				UpdateInstance();
			}
			else if (Input.GetKey(KeyCode.DownArrow))
			{
				Count--;
				UpdateInstance();
			}
			Graphics.DrawMeshInstancedIndirect(DMesh, 0, DMaterial, proxyBounds, argsBuffer);
		}
		private void OnDisable()
		{
			argsBuffer?.Release();
		}
	}
}