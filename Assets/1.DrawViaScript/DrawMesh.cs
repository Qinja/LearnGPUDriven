using UnityEngine;

namespace DrawViaScript
{
    public class DrawMesh : MonoBehaviour
    {
        public Mesh DMesh;
        public Material DMaterial;
        void Update()
        {
            var model = transform.localToWorldMatrix;
            Graphics.DrawMesh(DMesh, model, DMaterial, 0);
        }
    }
}