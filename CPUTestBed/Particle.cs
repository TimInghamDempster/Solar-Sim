using System;

namespace CPUTestBed
{
    public class Int2
    {
        public int X { get; set; }
        public int Y { get; set; }
        public static Int2 operator +(Int2 a, Int2 b)
            => new Int2()
            {
                X = a.X + b.X,
                Y = a.Y + b.Y,
            };
    }

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

        public static Vector3 operator *(Vector3 a, float b)
            => new Vector3()
            {
                X = a.X * b,
                Y = a.Y * b,
                Z = a.Z * b
            };
        public static Vector3 operator /(Vector3 a, float b)
            => new Vector3()
            {
                X = a.X / b,
                Y = a.Y / b,
                Z = a.Z / b
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

        public float Mass { get; set; }

        public int Id { get; }

        public Particle(Random random, float size, float offsetX, float offsetY)
        {
            Position = new Vector3(random, size, offsetX, offsetY);
            Velocity = new Vector3(random, 1f, -0.5f, -0.5f);
            Id = random.Next(0, int.MaxValue);
        }
        public Particle(Random random, Particle toReplicate)
        {
            Position = new Vector3()
            {
                X = toReplicate.Position.X + (float)random.NextDouble(),
                Y = toReplicate.Position.Y + (float)random.NextDouble(),
            };
            Velocity = new Vector3()
            {
                X = toReplicate.Velocity.X + (float)random.NextDouble() * 0.01f - 0.005f,
                Y = toReplicate.Velocity.Y + (float)random.NextDouble() * 0.01f - 0.005f,
            };
            Id = random.Next(0, int.MaxValue);
            Mass = toReplicate.Mass / 2;
            toReplicate.Mass = Mass;
        }
        public Particle(Particle toReplicate)
        {
            Position = new Vector3()
            {
                X = toReplicate.Position.X,
                Y = toReplicate.Position.Y,
            };
            Velocity = new Vector3()
            {
                X = toReplicate.Velocity.X,
                Y = toReplicate.Velocity.Y,
            };
            Id = toReplicate.Id;
            Mass = toReplicate.Mass;
        }
    }
}
