using UnityEngine;

namespace BackFaceCulling
{
    [System.Serializable]
    public struct BoundingSphere
    {
        public Vector3 center;
        public float radius;
        public const int SIZE = 3 * sizeof(float) + sizeof(float);
        public BoundingSphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
        public BoundingSphere(Vector3[] points)
        {
            var aabb = new Bounds(points[0], Vector3.zero);
            var count = points.Length;
            for (int i = 1; i < count; i++)
            {
                aabb.Encapsulate(points[i]);
            }
            this.center = aabb.center;
            this.radius = 0;
            for (int i = 1; i < count; i++)
            {
                var sqrLength = (points[i] - this.center).sqrMagnitude;
                this.radius = Mathf.Max(sqrLength, this.radius);
            }
            this.radius = Mathf.Sqrt(this.radius);
        }
        public void Expand(float amount)
        {
            this.radius += amount;
        }
        public void Encapsulate(Vector3 point)
        {
            if (!Contains(point))
            {
                var dir = (this.center - point).normalized;
                var far = this.center + dir * this.radius;
                this.center = 0.5f * (far + point);
                this.radius = (far - this.center).magnitude;
            }
        }
        public bool Contains(Vector3 point)
        {
            var sqrLength = (point - this.center).sqrMagnitude;
            return sqrLength <= this.radius * this.radius;
        }
    }
}