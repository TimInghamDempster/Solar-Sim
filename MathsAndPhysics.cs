using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace Micro_Architecture
{
    static class MathsAndPhysics
    {
        public const float DenseCoreSizeMilliPC = 100.0f;
        public const float AngularSpeedMicroRadsPerYear = 3.1f;

        public const float TimestepInYears = 0.01f;

        public static Vector3 AxisOfRotation;

        public static Random Random = new Random();

        /// <summary>
        /// Generates a random number drawn from a normal
        /// distibution with mean of zero and sd of one
        /// </summary>
        /// <returns></returns>
        public static double NextGaussianDouble()
        {
            double u, v, S;

            do
            {
                u = 2.0 * Random.NextDouble() - 1.0;
                v = 2.0 * Random.NextDouble() - 1.0;
                S = u * u + v * v;
            }
            while (S >= 1.0);

            double fac = Math.Sqrt(-2.0 * Math.Log(S) / S);
            return u * fac;
        }

        /// <summary>
        /// Creates a vector with each element drawn from a uniform
        /// distribution between 1 and -1
        /// </summary>
        /// <returns></returns>
        public static Vector3 GenerateRandomVec3()
        {
            Vector3 ret = new Vector3();

            ret.X = (float)Random.NextDouble() - 0.5f;
            ret.Y = (float)Random.NextDouble() - 0.5f;
            ret.Z = (float)Random.NextDouble() - 0.5f;

            ret *= 2.0f;

            return ret;
        }
    }
}
