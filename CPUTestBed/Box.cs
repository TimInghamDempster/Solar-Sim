using System;

namespace CPUTestBed
{
    public class Box
    {
        public Particle[] Particles { get; } = new Particle[64];
        public byte R { get; }
        public byte G { get; }

        public Box(Random random, float size, float x, float y)
        {
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i] = new Particle(random, size, x, y);
            }

            R = (byte)(x / 4);
            G = (byte)(y / 4);
        }
    }
}
