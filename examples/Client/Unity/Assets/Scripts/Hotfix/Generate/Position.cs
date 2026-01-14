using UnityEngine;

namespace Fantasy
{
    public partial class Position
    {
        public Vector3 UnityPosition => new(X, Y, Z);
        
        public Position Transform(Vector3 position)
        {
            this.X =  position.x;
            this.Y =  position.y;
            this.Z =  position.z;
            return this;
        }
    }
}