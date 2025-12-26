using System.Numerics;

namespace Fantasy
{
    public partial class Position
    {
        public Position Transform(Vector3 position)
        {
            this.X =  position.X;
            this.Y =  position.Y;
            this.Z =  position.Z;
            return this;
        }
    }
}