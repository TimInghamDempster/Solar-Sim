using SlimDX;
using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.HybridFluid
{
    public class ParticleBuffers : System.IDisposable
    {
        struct Particle
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Density;

            public const int Size = (3 + 3 + 1) * sizeof(float);
            public const string ShaderDefinition =
                "float3 position;\n"  +
                "\tfloat3 velocity;\n" +
                "\tfloat density;";
        }

        private readonly FlipFlop<ShaderResourceView> _readBuffer;
        private readonly FlipFlop<UnorderedAccessView> _writeBuffer;

        private Buffer _data1;
        private ShaderResourceView _srv1;
        private UnorderedAccessView _uav1;

        private Buffer _data2;
        private ShaderResourceView _srv2;
        private UnorderedAccessView _uav2;

        public IContext<ShaderResourceView> ReadBuffer => null; //_readBuffer;
        public IContext<UnorderedAccessView> WriteBuffer => null; // _writeBuffer;

        public const string ShaderDefinition = Particle.ShaderDefinition;
        
        /// <summary>
        /// Maintains the data buffers for the particles.  The
        /// particles are double buffered and the views onto the
        /// buffers flip-flop which is the read and which the write
        /// so that the references given to the shaders are constant
        /// whilst the buffers shuttle data back and forth via whatever
        /// transform a consuming shader applies
        /// </summary>
        public ParticleBuffers(
            Device device,
            Vector3 scale,
            Vector3 offset,
            int numParticles)
        {
            BuildBuffers(scale, offset, numParticles, device);

            _readBuffer = 
                new FlipFlop<ShaderResourceView>(
                    _srv1,
                    _srv2);

            _writeBuffer =
                new FlipFlop<UnorderedAccessView>(
                    _uav2,
                    _uav1);
        }

        private void BuildBuffers(
            Vector3 scale, 
            Vector3 offset,
            int numParticles,
            Device device)
        {
            // Size calculations
            int bufferSize = numParticles * Particle.Size;

            // Create a stream and fill it with data
            var streamB = new DataStream(bufferSize, true, true);
            var streamA = new DataStream(bufferSize, true, true);

            for (int particleId = 0; particleId < numParticles; particleId++)
            {
                var particle = new Particle()
                {
                    Position = MathsAndPhysics.GenerateRandomVec3().ComponentMultiply(scale) + offset,
                    Velocity = MathsAndPhysics.GenerateRandomVec3(),
                    Density = 0.0f
                };

                particle.Velocity.Z = 0.0f;

                streamA.Write(particle);
                streamB.Write(particle);
            }

            streamB.Position = 0;
            streamA.Position = 0;

            // Create the buffer and fill it with data
            BufferDescription desc = new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = bufferSize,
                StructureByteStride = Particle.Size,
                Usage = ResourceUsage.Default
            };

            _data1 = new Buffer(device, streamA, desc);
            _srv1 = new ShaderResourceView(device, _data1);
            _uav1 = new UnorderedAccessView(device, _data1);

            _data2 = new Buffer(device, streamB, desc);
            _srv2= new ShaderResourceView(device, _data2);
            _uav2 = new UnorderedAccessView(device, _data2);
        }

        public void Tick()
        {
            _readBuffer.Tick();
            _writeBuffer.Tick();
        }

        public void Dispose()
        {
            _srv1.Dispose();
            _srv2.Dispose();

            _uav1.Dispose();
            _uav2.Dispose();

            _data1.Dispose();
            _data2.Dispose();
        }
    }
}
