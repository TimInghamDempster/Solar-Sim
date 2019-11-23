using SlimDX.Direct3D11;

namespace SolarSim.HybridFluid
{
    public class SubspaceBuffers : System.IDisposable
    {
        public const int ParticlesPerBox = 8;
        public const int BoxSize = (3 + 3 + 1) * ParticlesPerBox * sizeof(float) + sizeof(int);
        public const string BoxDefinition =
            "int count;\n" +
            "\tfloat3 positions[8];\n" +
            "\tfloat densities[8];\n" +
            "\tfloat3 velocities[8];\n";

        private readonly Buffer _data;

        public ShaderResourceView ReadBuffer { get; }
        public UnorderedAccessView WriteBuffer { get; }

        public SubspaceBuffers(
            Device device,
            int boxCountX,
            int boxCountY,
            int boxCountZ)
        {
            var bufferSize = boxCountX * boxCountY * boxCountZ * BoxSize;

            BufferDescription desc = new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = bufferSize,
                StructureByteStride = BoxSize,
                Usage = ResourceUsage.Default
            };

            _data = new Buffer(device, desc);
            ReadBuffer = new ShaderResourceView(device, _data);
            WriteBuffer = new UnorderedAccessView(device, _data);
        }

        public void Dispose()
        {
            _data.Dispose();
            ReadBuffer.Dispose();
            WriteBuffer.Dispose();
        }
    }
}
