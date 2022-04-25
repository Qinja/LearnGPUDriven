using UnityEngine;

namespace DrawViaScript
{
	public class DrawMeshInstancedMPB : MonoBehaviour
	{
		public Mesh DMesh;
		public Material DMaterial;
		private Matrix4x4[] models;
		private MaterialPropertyBlock mpb;
		void Start()
		{
			models = new Matrix4x4[25];
			var colors = new Vector4[25];
			var parentPosition = transform.position;
			for (int i = 0, k = 0; i < 5; i++)
			{
				for (int j = 0; j < 5; j++, k++)
				{
					var pos = parentPosition + new Vector3(0, i * 2, j * 2);
					models[k] = Matrix4x4.Translate(pos);
					colors[k] = new Vector4(Random.Range(0.0f, 0.8f), Random.value, Random.value, 1.0f);
				}
			}
			mpb = new MaterialPropertyBlock();
			mpb.SetVectorArray("_Color", colors);
		}
		void Update()
		{
			Graphics.DrawMeshInstanced(DMesh, 0, DMaterial, models, 25, mpb);
		}
	}
}