using SlimDX;

namespace SlimDXHelpers
{
    public static class VectorExtensions
    {
        public static Vector3 ComponentMultiply(this Vector3 lhs, Vector3 rhs)
        {
            // Vector3 is a struct so modifying lhs doesn't modify
            // the original passed to us
            lhs.X *= rhs.X;
            lhs.Y *= rhs.Y;
            lhs.Z *= rhs.Z;

            return lhs;
        }
    }
}
