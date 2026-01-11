using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using LightProto;
using MongoDB.Bson.Serialization.Attributes;

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

        public void Transform(ref Vector3 position)
        {
            position.X = X;
            position.Y = Y;
            position.Z = Z;
        }
    }
}