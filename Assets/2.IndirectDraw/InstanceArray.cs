using UnityEngine;

namespace IndirectDraw
{
    public class InstanceArray : MonoBehaviour
    {
        public Mesh DMesh;
        public Material DMaterial;
        [Range(1, 1023)]
        public int Count = 100;

        private Matrix4x4[] models;
        private MaterialPropertyBlock mpb;
        void Start()
        {
            UpdateInstance();
        }
        void UpdateInstance()
        {
            if (Count < 1) Count = 1;
            if (Count > 1023) Count = 1023;
            models = new Matrix4x4[Count];
            var colors = new Vector4[Count];
            var parentPosition = transform.position;
            int row = Mathf.FloorToInt(Mathf.Sqrt(Count - 1)) + 1;
            Random.InitState(0);
            for (int i = 0, k = 0; i < row; i++)
            {
                for (int j = 0; j < row && k < Count; j++, k++)
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
            Graphics.DrawMeshInstanced(DMesh, 0, DMaterial, models, Count, mpb);
        }
    }
}