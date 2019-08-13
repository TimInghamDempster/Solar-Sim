using SlimDX;
using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.HybridFluid
{
    public class ParticleBuffers : System.IDisposable
    {
        private readonly Device _device;

        private readonly FlipFlop<ShaderResourceView> _readBuffer;
        private readonly FlipFlop<UnorderedAccessView> _writeBuffer;

        private Buffer _data1;
        private ShaderResourceView _srv1;
        private UnorderedAccessView _uav1;

        private Buffer _data2;
        private ShaderResourceView _srv2;
        private UnorderedAccessView _uav2;

        public IContext<ShaderResourceView> ReadBuffer => _readBuffer;
        public IContext<UnorderedAccessView> WriteBuffer => _writeBuffer;

        public int ParticleSize =>
            3 * sizeof(float) * 3 + sizeof(float); // float3 position + float3 velocity + float3 colour + float density

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
            _device = device;

            BuildBuffers(scale, offset, numParticles);

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
            int numParticles)
        {
            // Size calculations
            int bufferSize = numParticles * ParticleSize;

            // Create a stream and fill it with data
            var streamB = new DataStream(bufferSize, true, true);
            var streamA = new DataStream(bufferSize, true, true);

            for (int particleId = 0; particleId < numParticles; particleId++)
            {
                var pos = MathsAndPhysics.GenerateRandomVec3();
                var colour = Vector3.Zero;
                var vel = MathsAndPhysics.GenerateRandomVec3();
                colour.X = 1.0f;//(pos.X / 2.0f) + 0.5f;
                //colour.Y = (pos.Y / 2.0f) + 0.5f;
                //colour.Z = (pos.Z / 2.0f) + 0.5f;
                pos.X *= scale.X;
                pos.Y *= scale.Y;
                pos.Z *= scale.Z;
                pos += offset;

                streamB.Write(pos);
                streamB.Write(vel);
                streamB.Write(colour);
                streamB.Write(0.0f);

                streamA.Write(pos);
                streamA.Write(vel);
                streamA.Write(colour);
                streamA.Write(0.0f);
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
                StructureByteStride = ParticleSize,
                Usage = ResourceUsage.Default
            };

            _data1 = new Buffer(_device, streamA, desc);
            _srv1 = new ShaderResourceView(_device, _data1);
            _uav1 = new UnorderedAccessView(_device, _data1);

            _data2 = new Buffer(_device, streamB, desc);
            _srv2= new ShaderResourceView(_device, _data2);
            _uav2 = new UnorderedAccessView(_device, _data2);
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
