using UnityEngine;

namespace OcclusionCulling
{
	public class PreZDebugger : MonoBehaviour
	{
		public Camera DCamera;
		public Material DMaterial;
		public int InitHash;

		private Bounds proxyBounds;
		private ComputeBuffer debugBuffer;
		void Start()
		{
			proxyBounds = new Bounds(Vector3.zero, 1000.0f * Vector3.one);
			debugBuffer = new ComputeBuffer(1, sizeof(uint));
			Graphics.SetRandomWriteTarget(1, debugBuffer);
			if (InitHash == 0)
			{
				InitHash = Random.Range(0x12341234, 0x67896789);
			}
			debugBuffer.SetData(new uint[1] { (uint)InitHash });
		}
		void Update()
		{
			uint[] debugData = new uint[1];
			debugBuffer.GetData(debugData);
			if (debugData[0] == InitHash)
			{
				Debug.Log("EarlyZ is vaild");
			}
			else
			{

				Debug.LogError("EarlyZ is invaild");
			}
			int randomHash = Random.Range(0x12341234, 0x67896789);
			if (randomHash == InitHash)
			{
				randomHash = InitHash + 0x00120012;
			}
			DMaterial.SetInt("_Hash", randomHash);
			Graphics.DrawProcedural(DMaterial, proxyBounds, MeshTopology.Triangles, 3, 1, DCamera);
		}
		private void OnDisable()
		{
			debugBuffer?.Release();
		}
	}
}