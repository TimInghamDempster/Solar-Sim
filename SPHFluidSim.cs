using System;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using SlimDX;

namespace Micro_Architecture
{
    class SPHFluidSim : ISimulation
    {
        private Device _device;
        ComputeShader _particleOutputCS;
        ComputeShader _pressureOutputCS;
        ComputeShader _updateParticlePosCS;
        UnorderedAccessView _outputUAV;
        SlimDX.Direct3D11.Buffer _particleBufferA;
        SlimDX.Direct3D11.Buffer _particleBufferB;
        ShaderResourceView _particleBufferSRVA;
        ShaderResourceView _particleBufferSRVB;
        UnorderedAccessView _particleBufferUAVA;
        UnorderedAccessView _particleBufferUAVB;

        const int _numBoxesPerAxis = 64;
        const int _particlesPerBox = 16;
        private int _renderWidth;
        private int _renderHeight;

        public void Init(Device device, UnorderedAccessView outputUAV,int renderWidth, int renderHeight)
        {
            _device = device;
            _outputUAV = outputUAV;
            _particleOutputCS = BuildComputeShader("OutputParticles");
            _pressureOutputCS = BuildComputeShader("OutputPressures");
            _updateParticlePosCS = BuildComputeShader("UpdateParticlePositions");
            _renderHeight = renderHeight;
            _renderWidth = renderWidth;

            InitParticleBuffer();
        }

        /// <summary>
        /// Build the buffer that will hold the particles, a 3d grid of
        /// boxes
        /// </summary>
        private void InitParticleBuffer()
        {
            var rand = MathsAndPhysics.Random;
            // Size calculations
            const int particleSize = 3 * sizeof(float) * 2 + sizeof(float); // float3 position + float3 velocity + float mass
            const int numBoxes = _numBoxesPerAxis * _numBoxesPerAxis * _numBoxesPerAxis;
            const int numParticles = numBoxes * _particlesPerBox;
            const int bufferSize = numParticles * particleSize;

            // Create a stream and fill it with data
            var streamB = new DataStream(bufferSize, true, true);

            //MathsAndPhysics.AxisOfRotation = MathsAndPhysics.GenerateRandomVec3();
            //MathsAndPhysics.AxisOfRotation.Normalize();

            for (int particleId = 0; particleId < numParticles; particleId++)
            {
                var pos = MathsAndPhysics.GenerateRandomVec3() / 2.0f;
                pos += new Vector3(0.5f);
                streamB.Write(pos);

                var velocity = MathsAndPhysics.GenerateRandomVec3() * 0.1f;
                streamB.Write(velocity);
                //streamB.Write(Vector3.UnitX);

                streamB.Write(1.0f);
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

            var streamA = new DataStream(bufferSize, true, true);

            _particleBufferA = new SlimDX.Direct3D11.Buffer(_device, streamA, desc);
            _particleBufferSRVA = new ShaderResourceView(_device, _particleBufferA);
            _particleBufferUAVA = new UnorderedAccessView(_device, _particleBufferA);

            _particleBufferB = new SlimDX.Direct3D11.Buffer(_device, streamB, desc);
            _particleBufferSRVB = new ShaderResourceView(_device, _particleBufferB);
            _particleBufferUAVB = new UnorderedAccessView(_device, _particleBufferB);
        }

        public void SimMain(int frameCount)
        {
            UpdateParticlePositions();
            //OutputParticles();
            OutputPresure();
            SwapBuffers();
        }
        private void UpdateParticlePositions()
        {
            _device.ImmediateContext.ComputeShader.Set(_updateParticlePosCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_particleBufferUAVA, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 1);
            _device.ImmediateContext.ClearUnorderedAccessView(_particleBufferUAVA, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_numBoxesPerAxis / 4, _numBoxesPerAxis / 4, _numBoxesPerAxis / 4);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }

        private void OutputParticles()
        {
            _device.ImmediateContext.ComputeShader.Set(_particleOutputCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_outputUAV, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 1);
            _device.ImmediateContext.ClearUnorderedAccessView(_outputUAV, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_numBoxesPerAxis, _numBoxesPerAxis, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }

        private void OutputPresure()
        {
            _device.ImmediateContext.ComputeShader.Set(_pressureOutputCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_outputUAV, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 1);
            _device.ImmediateContext.ClearUnorderedAccessView(_outputUAV, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_renderHeight / 16, _renderHeight / 16, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }

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

        private ComputeShader BuildComputeShader(string shaderName)
        {
            var csBytecode = ShaderBytecode.CompileFromFile("SPHFluidComputeShaders.hlsl", shaderName, "cs_5_0", ShaderFlags.None, EffectFlags.None);
            var computeShader = new ComputeShader(_device, csBytecode);

            return computeShader;
        }
    }
}
