using System;

namespace CPUTestBed
{
    public class Box
    {
        public Particle[] Particles { get; }
        public byte R { get; }
        public byte G { get; }
        public Vector3 Lower { get; }
        public Vector3 Upper { get; }

        public Box(Random random, Vector3 lower, Vector3 upper, int particleCount, Particle nullParticle)
        {

            Particles = new Particle[particleCount];
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i] = new Particle(nullParticle);
                //Particles[i] = new Particle(random, upper.X - lower.X, lower.X, lower.Y);
            }

            R = (byte)(lower.X / 4);
            G = (byte)(lower.Y / 4);

            Lower = lower;
            Upper = upper;
        }
    }
}
