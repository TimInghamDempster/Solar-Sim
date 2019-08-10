/*using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System;

namespace Micro_Architecture
{
    class HybridPressureFluid : ISimulation
    {
        SlimDX.Direct3D11.Device _device;
        ComputeShader _particlePointOutputCS;
        ComputeShader _updateParticlePositionsCS;
        ComputeShader _fillParticleBoxesCS;
        UnorderedAccessView _renderUAV;
        SlimDX.Direct3D11.Buffer _particleBufferA;
        SlimDX.Direct3D11.Buffer _particleBufferB;
        ShaderResourceView _particleBufferSRVA;
        ShaderResourceView _particleBufferSRVB;
        UnorderedAccessView _particleBufferUAVA;
        UnorderedAccessView _particleBufferUAVB;
        SlimDX.Direct3D11.Buffer _boxes;
        ShaderResourceView _boxesSRV;
        UnorderedAccessView _boxesUAV;
        SlimDX.Direct3D11.Buffer _physicsConstantBuffer;
        SlimDX.Direct3D11.Buffer _displacementBuffer;
        Vector4 _displacerPosition;
        Vector4 _displacerVelocity;
        const bool _mainRenderParticles = true;

        bool _holdDamn = true;

        // We store particles in a grid of boxes, this
        // gives us a global way to control it
        const int _numBoxesPerAxis = 64;
        const int _numBoxesTotal = _numBoxesPerAxis * _numBoxesPerAxis * _numBoxesPerAxis;

        /// <summary>
        /// We now need to swap the buffers so that the input to the
        /// next timestep is the output from the last one
        /// </summary>
        private void SwapBuffers()
        {
            var tempSRV = _particleBufferSRVA;
            _particleBufferSRVA = _particleBufferSRVB;
            _particleBufferSRVB = tempSRV;

            var tempUAV = _particleBufferUAVA;
            _particleBufferUAVA = _particleBufferUAVB;
            _particleBufferUAVB = tempUAV;
        }

        private void InitPhysicsConstantBuffer()
        {
            var physicsBufferSizeInBytes = 32;
            var desc = new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = physicsBufferSizeInBytes,
                StructureByteStride = 0,
                Usage = ResourceUsage.Default
            };

            var data = new DataStream(physicsBufferSizeInBytes, true, true);
            data.Write(MathsAndPhysics.TimestepInYears);
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f);
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f);
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC / 128.0f);
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC);
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC);
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC / 64.0f);
            data.Write((float)_numBoxesPerAxis);

            data.Position = 0;

            _physicsConstantBuffer = new SlimDX.Direct3D11.Buffer(_device, data, desc);

            var displacementData = new DataStream(16, true, true);
            var pos = MathsAndPhysics.GenerateRandomVec3() * MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f;
            _displacerPosition.X = pos.X;
            _displacerPosition.Y = pos.Y;
            _displacerPosition.Z = 0.0f;
            displacementData.Write(_displacerPosition);

            var vel = MathsAndPhysics.GenerateRandomVec3();
            vel.Z = 0.0f;
            vel.Normalize();
            vel *= ((MathsAndPhysics.DenseCoreSizeMilliPC / _numBoxesPerAxis) * 0.9f);
            _displacerVelocity.X = vel.X;
            _displacerVelocity.Y = vel.Y;
            _displacerVelocity.Z = 0.0f;

            displacementData.Position = 0;
            var displacementDesc = new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 16,
                StructureByteStride = 0,
                Usage = ResourceUsage.Dynamic
            };
            _displacementBuffer = new SlimDX.Direct3D11.Buffer(_device, displacementData, displacementDesc);
        }

        private ComputeShader BuildComputeShader(string shaderName)
        {
            var csBytecode = ShaderBytecode.CompileFromFile("HybridFluidComputeShaders.hlsl", shaderName, "cs_5_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization, EffectFlags.None);
            var computeShader = new ComputeShader(_device, csBytecode);

            return computeShader;
        }

        public void Init(SlimDX.Direct3D11.Device device, UnorderedAccessView outputUAV, int renderWidth, int renderHeight)
        {
            _device = device;
            _renderUAV = outputUAV;

            InitParticleBuffer();
            InitPhysicsConstantBuffer();

            _particlePointOutputCS = BuildComputeShader("OutputParticlePoints");
            _updateParticlePositionsCS = BuildComputeShader("UpdateParticlePositions");
            _fillParticleBoxesCS = BuildComputeShader("WriteParticlesToBoxes");
        }

        public void SimMain(int frameCount)
        {
            WriteParticlesToBoxes();
            UpdateParticleState();

            if (_mainRenderParticles == true)
            {
                OutputParticlePositions();
                //OutputPressurePoints();
            }

            SwapBuffers();

            if (_holdDamn)
            {
                //DoMoveableBoundary();

                //SwapBuffers();
            }
        }
        

        private void WriteParticlesToBoxes()
        {
            _device.ImmediateContext.ComputeShader.Set(_fillParticleBoxesCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessViews(
                new UnorderedAccessView[] { _boxesUAV }, 0, 1);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 2);

            _device.ImmediateContext.ComputeShader.SetConstantBuffer(_physicsConstantBuffer, 0);
            _device.ImmediateContext.ClearUnorderedAccessView(_boxesUAV, new int[] { 0, 0, 0, 0 });
            _device.ImmediateContext.Dispatch(_numBoxesPerAxis, 1, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 2);
        }

        /// <summary>
        /// Build the buffer that will hold the particles, a 3d grid of
        /// boxes
        /// </summary>
        private void InitParticleBuffer()
        {
            var rand = MathsAndPhysics.Random;
            // Size calculations
            const int particleSize = 3 * sizeof(float) * 3 + sizeof(float); // float3 position + float3 velocity + float3 colour + float density
            const int particlesPerBox = 8;
            const int numBoxes = _numBoxesPerAxis * _numBoxesPerAxis * 1;
            const int numParticles = _numBoxesPerAxis * particlesPerBox; // numBoxes * particlesPerBox;
            const int bufferSize = numParticles * particleSize;

            // Create a stream and fill it with data
            var streamB = new DataStream(bufferSize, true, true);
            var streamA = new DataStream(bufferSize, true, true);

            MathsAndPhysics.AxisOfRotation = MathsAndPhysics.GenerateRandomVec3();
            MathsAndPhysics.AxisOfRotation.Normalize();

            const int numEnergeticParticles = 00000;
            for (int particleId = 0; particleId < numParticles - numEnergeticParticles; particleId++)
            {
                var pos = MathsAndPhysics.GenerateRandomVec3();
                var colour = Vector3.Zero;
                colour.X = 1.0f;//(pos.X / 2.0f) + 0.5f;
                colour.Y = (pos.Y / 2.0f) + 0.5f;
                colour.Z = 0.0f;// (pos.Z / 2.0f) + 0.5f;
                pos *= MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f;
                pos.Z /= 64.0f;
                pos.X /= 4;
                pos.X += MathsAndPhysics.DenseCoreSizeMilliPC / 8.0f * 3.0f;

                streamB.Write(pos);

                var direction = Vector3.Cross(pos, MathsAndPhysics.AxisOfRotation);
                direction.Normalize();
                float speed = 2.0f * (float)Math.PI * MathsAndPhysics.DenseCoreSizeMilliPC * MathsAndPhysics.AngularSpeedMicroRadsPerYear * 1000.0f;
                var velocityMilliPCPerYear = direction * speed;
                //var velocity = MathsAndPhysics.GenerateRandomVec3() * 100.0f;// Vector3.Zero;//  Vector3.UnitX * 2 + Vector3.UnitY + (MathsAndPhysics.GenerateRandomVec3() * 0.1f);
                var velocity = (Vector3.UnitX * 2 + Vector3.UnitY + (MathsAndPhysics.GenerateRandomVec3() * 0.1f)) * 100.0f;
                //streamB.Write(velocity * 50);
                streamB.Write(Vector3.Zero);

                streamB.Write(colour);

                streamB.Write(0.0f);

                streamA.Write(Vector3.Zero);
                streamA.Write(Vector3.Zero);
                streamA.Write(colour);
            }

            var energeticParticleStartPos = MathsAndPhysics.GenerateRandomVec3();
            energeticParticleStartPos *= MathsAndPhysics.DenseCoreSizeMilliPC / 3.0f;
            energeticParticleStartPos.Z = 0.0f;
            var energeticDirection = MathsAndPhysics.GenerateRandomVec3();
            energeticDirection.Z = 0.0f;
            float energeticSpeed = (MathsAndPhysics.DenseCoreSizeMilliPC / _numBoxesPerAxis) / MathsAndPhysics.TimestepInYears;

            for (int particleId = 0; particleId < numEnergeticParticles / 2; particleId++)
            {
                var pos = MathsAndPhysics.GenerateRandomVec3();
                pos *= MathsAndPhysics.DenseCoreSizeMilliPC / 50.0f;
                pos += energeticParticleStartPos;
                streamB.Write(pos);

                var direction = MathsAndPhysics.GenerateRandomVec3();
                direction.Z = 0.0f;
                direction.Normalize();
                // float speed = (MathsAndPhysics.DenseCoreSizeMilliPC / NumBoxesPerAxis) / MathsAndPhysics.TimestepInYears;
                var velocityMilliPCPerYear = energeticDirection * energeticSpeed;
                var velocity = velocityMilliPCPerYear;// Vector3.UnitX * 2 + Vector3.UnitY + (MathsAndPhysics.GenerateRandomVec3() * 0.1f);
                streamB.Write(velocity);
                //streamB.Write(velocityMilliPCPerYear);
            }
            // Have to reset the stream or buffer creation will try to read from
            // the end
            streamB.Position = 0;

            // Create the buffer and fill it with data
            BufferDescription desc = new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                SizeInBytes = bufferSize,
                StructureByteStride = particleSize,
                Usage = ResourceUsage.Default
            };
            streamA.Position = 0;

            _particleBufferA = new SlimDX.Direct3D11.Buffer(_device, streamA, desc);
            _particleBufferSRVA = new ShaderResourceView(_device, _particleBufferA);
            _particleBufferUAVA = new UnorderedAccessView(_device, _particleBufferA);

            _particleBufferB = new SlimDX.Direct3D11.Buffer(_device, streamB, desc);
            _particleBufferSRVB = new ShaderResourceView(_device, _particleBufferB);
            _particleBufferUAVB = new UnorderedAccessView(_device, _particleBufferB);

            var boxSizeInBytes = 4 + (4 * 7 * particlesPerBox);
            desc.SizeInBytes = boxSizeInBytes * numBoxes;
            desc.StructureByteStride = boxSizeInBytes;

            _boxes = new SlimDX.Direct3D11.Buffer(_device, desc);
            _boxesSRV = new ShaderResourceView(_device, _boxes);
            _boxesUAV = new UnorderedAccessView(_device, _boxes);
        }

        // Do the calculations for the current timestep,
        // update positions and forces of particles
        private void UpdateParticleState()
        {
            _device.ImmediateContext.ComputeShader.Set(_updateParticlePositionsCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessViews(
                new UnorderedAccessView[] { _particleBufferUAVA }, 0, 1);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 2);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_boxesSRV, 3);

            _device.ImmediateContext.ComputeShader.SetConstantBuffer(_physicsConstantBuffer, 0);

            _device.ImmediateContext.Dispatch(_numBoxesPerAxis, 1, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 2);
        }

        private void OutputParticlePositions()
        {
            _device.ImmediateContext.ComputeShader.Set(_particlePointOutputCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_renderUAV, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 2);
            _device.ImmediateContext.ClearUnorderedAccessView(_renderUAV, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_numBoxesPerAxis, 1, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 1);
        }

        public void LeftClick()
        {
            _holdDamn = false;
        }
    }
}
*/