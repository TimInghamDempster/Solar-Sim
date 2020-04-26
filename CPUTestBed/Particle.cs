using System;

namespace CPUTestBed
{
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static Vector3 operator +(Vector3 a, Vector3 b)
            => new Vector3()
            {
                X = a.X + b.X,
                Y = a.Y + b.Y,
                Z = a.Z + b.Z
            };


        public Vector3() { }

        public Vector3(Random random, float scale, float offsetX, float offsetY)
        {
            X = (float)random.NextDouble() * scale + offsetX;
            Y = (float)random.NextDouble() * scale + offsetY;
            Z = (float)random.NextDouble() * scale + 0.0f;
        }
    }

    public class Particle
    { 
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }

        public Particle(Random random, float size, float offsetX, float offsetY)
        {
            Position = new Vector3(random, size, offsetX, offsetY);
            Velocity = new Vector3(random, 10, -5, -5);
        }
    }
}
