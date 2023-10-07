using UnityEngine;

namespace BackFaceCulling
{
    [System.Serializable]
    public struct BoundingCone
    {
        public Vector3 normal;
        public float cosAngle;
        public float sinAngle;
        public const int SIZE = 3 * sizeof(float) + 2 * sizeof(float);
        public BoundingCone(Vector3 normal, float angleRad)
        {
            this.normal = normal;
            this.cosAngle = Mathf.Cos(angleRad);
            this.sinAngle = Mathf.Sin(angleRad);
        }
        public BoundingCone(Vector3[] directions) : this(directions[0], 0.0f)
        {
            var count = directions.Length;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    var cosAngle = Vector3.Dot(directions[i], directions[j]);
                    if (cosAngle < this.cosAngle)
                    {
                        this.cosAngle = cosAngle;
                        this.sinAngle = Mathf.Sqrt(1 - cosAngle * cosAngle);
                        this.normal = directions[i] + directions[j];
                    }
                }
            }
            if (this.normal == Vector3.zero)
            {
                this.normal = Vector3.one;
            }
            else
            {
                this.normal.Normalize();
                for (int i = 0; i < count; i++)
                {
                    Encapsulate(directions[i]);
                }
            }
        }
        public void Expand(float angleRad)
        {
            this.cosAngle = Mathf.Cos(Mathf.Min(Mathf.Acos(this.cosAngle) + angleRad, Mathf.PI));
        }
        public void Encapsulate(Vector3 direction)
        {
            if (!Contains(direction))
            {
                Vector3 y = Vector3.Cross(direction, this.normal).normalized;
                var beta = Vector3.Dot(this.normal, direction);
                var halfAngle = Mathf.Rad2Deg * 0.5f * (Mathf.Acos(this.cosAngle) + Mathf.Acos(beta));
                Quaternion rotation = Quaternion.AngleAxis(halfAngle, y);
                var halfDirection = rotation * direction;
                this.normal = halfDirection.normalized;
                this.cosAngle = Mathf.Cos(Mathf.Deg2Rad * halfAngle);
                this.sinAngle = Mathf.Sin(Mathf.Deg2Rad * halfAngle);
            }
        }
        public bool Contains(Vector3 direction)
        {
            return Vector3.Dot(direction, this.normal) >= this.cosAngle;
        }
    }
}