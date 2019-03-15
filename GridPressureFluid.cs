using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System;

namespace Micro_Architecture
{
    class GridPressureFluid : ISimulation
    {
        SlimDX.Direct3D11.Device _device;
        ComputeShader _particlePointOutputCS;
        ComputeShader _pressureOutputCS;
        ComputeShader _updateParticlePositionsCS;
        ComputeShader _calculatePressureGradientCS;
        ComputeShader _displacementObjectCS;
        UnorderedAccessView _renderParticalUAV;
        UnorderedAccessView _renderGridUAV;
        Texture3D _pressureTexture;
        UnorderedAccessView _pressureUAV;
        ShaderResourceView _pressureSRV;
        Texture3D _pressureGradientTexture;
        UnorderedAccessView _pressureGradientUAV;
        ShaderResourceView _pressureGradientSRV;
        SlimDX.Direct3D11.Buffer _particleBufferA;
        SlimDX.Direct3D11.Buffer _particleBufferB;
        ShaderResourceView _particleBufferSRVA;
        ShaderResourceView _particleBufferSRVB;
        UnorderedAccessView _particleBufferUAVA;
        UnorderedAccessView _particleBufferUAVB;
        SlimDX.Direct3D11.Buffer _physicsConstantBuffer;
        SlimDX.Direct3D11.Buffer _displacementBuffer;
        Vector4 _displacerPosition;
        Vector4 _displacerVelocity;
        const bool _mainRenderParticles = false;

        // We store particles in a grid of boxes, this
        // gives us a global way to control it
        const int _numBoxesPerAxis = 128;
        const int _numBoxesTotal = _numBoxesPerAxis * _numBoxesPerAxis * _numBoxesPerAxis;

        private void ApplyDisplacementObjects()
        {
            _displacerPosition += _displacerVelocity;

            if (_displacerPosition.X > MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f && _displacerVelocity.X > 0.0f)
            {
                _displacerVelocity.X *= -1.0f;
            }
            if (_displacerPosition.X < -MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f && _displacerVelocity.X < 0.0f)
            {
                _displacerVelocity.X *= -1.0f;
            }
            if (_displacerPosition.Y > MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f && _displacerVelocity.Y > 0.0f)
            {
                _displacerVelocity.Y *= -1.0f;
            }
            if (_displacerPosition.Y < -MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f && _displacerVelocity.Y < 0.0f)
            {
                _displacerVelocity.Y *= -1.0f;
            }
            if (_displacerPosition.Z > MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f && _displacerVelocity.Z > 0.0f)
            {
                _displacerVelocity.Z *= -1.0f;
            }
            if (_displacerPosition.Z < -MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f && _displacerVelocity.Z < 0.0f)
            {
                _displacerVelocity.Z *= -1.0f;
            }

            var displacementData = new DataStream(16, true, true);
            displacementData.Write(_displacerPosition);

            var dataBox = _device.ImmediateContext.MapSubresource(_displacementBuffer, 0, MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);
            dataBox.Data.Write<Vector4>(_displacerPosition);
            _device.ImmediateContext.UnmapSubresource(_displacementBuffer, 0);

            SwapBuffers();

            _device.ImmediateContext.ComputeShader.Set(_displacementObjectCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessViews(
                new UnorderedAccessView[] { _particleBufferUAVA, _pressureUAV }, 0, 2);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 2);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_pressureGradientSRV, 3);

            _device.ImmediateContext.ComputeShader.SetConstantBuffer(_physicsConstantBuffer, 0);
            _device.ImmediateContext.ComputeShader.SetConstantBuffer(_displacementBuffer, 1);


            _device.ImmediateContext.Dispatch(_numBoxesTotal, 1, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 2);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 1);
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

        private void InitPhysicsConstantBuffer()
        {
            var physicsBufferSizeInBytes = 64;
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
            data.Write(MathsAndPhysics.DenseCoreSizeMilliPC);
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
            var csBytecode = ShaderBytecode.CompileFromFile("GridPressureFluidComputeShaders.hlsl", shaderName, "cs_5_0", ShaderFlags.None, EffectFlags.None);
            var computeShader = new ComputeShader(_device, csBytecode);

            return computeShader;
        }

        public void Init(SlimDX.Direct3D11.Device device, UnorderedAccessView outputUAV)
        {
            _device = device;
            _renderGridUAV = outputUAV;

            InitParticleBuffer();
            InitPressureTexture();
            InitPhysicsConstantBuffer();

            _particlePointOutputCS = BuildComputeShader("OutputParticlePoints");
            _pressureOutputCS = BuildComputeShader("OutputPressures");
            _calculatePressureGradientCS = BuildComputeShader("CalculatePressureGradients");
            _updateParticlePositionsCS = BuildComputeShader("UpdateParticelPositions");
            _displacementObjectCS = BuildComputeShader("ApplyDisplacementForce");
        }

        public void SimMain()
        {
            UpdateParticleState();
            CalculatePressureGradients();
            ApplyDisplacementObjects();

            if (_mainRenderParticles == true)
            {
                OutputParticlePositions();
            }
            else
            {
                OutputPressure();
            }

            SwapBuffers();
        }

        /// <summary>
        /// Build the buffer that will hold the particles, a 3d grid of
        /// boxes
        /// </summary>
        private void InitParticleBuffer()
        {
            var rand = MathsAndPhysics.Random;
            // Size calculations
            const int particleSize = 3 * sizeof(float) * 2; // float3 position + float3 velocity
            const int particlesPerBox = 16;
            const int numBoxes = _numBoxesPerAxis * _numBoxesPerAxis * _numBoxesPerAxis;
            const int numParticles = numBoxes * particlesPerBox;
            const int bufferSize = numParticles * particleSize;

            // Create a stream and fill it with data
            var streamB = new DataStream(bufferSize, true, true);

            MathsAndPhysics.AxisOfRotation = MathsAndPhysics.GenerateRandomVec3();
            MathsAndPhysics.AxisOfRotation.Normalize();

            const int numEnergeticParticles = 00000;

            for (int particleId = 0; particleId < numParticles - numEnergeticParticles; particleId++)
            {
                var pos = MathsAndPhysics.GenerateRandomVec3();
                pos *= MathsAndPhysics.DenseCoreSizeMilliPC / 2.0f;
                streamB.Write(pos);

                var direction = Vector3.Cross(pos, MathsAndPhysics.AxisOfRotation);
                direction.Normalize();
                float speed = 2.0f * (float)Math.PI * MathsAndPhysics.DenseCoreSizeMilliPC * MathsAndPhysics.AngularSpeedMicroRadsPerYear / 1000000.0f;
                var velocityMilliPCPerYear = direction * speed;
                var velocity = Vector3.Zero;// Vector3.UnitX * 2 + Vector3.UnitY + (MathsAndPhysics.GenerateRandomVec3() * 0.1f);
                streamB.Write(velocity * 50);
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


            var streamA = new DataStream(bufferSize, true, true);

            _particleBufferA = new SlimDX.Direct3D11.Buffer(_device, streamA, desc);
            _particleBufferSRVA = new ShaderResourceView(_device, _particleBufferA);
            _particleBufferUAVA = new UnorderedAccessView(_device, _particleBufferA);

            _particleBufferB = new SlimDX.Direct3D11.Buffer(_device, streamB, desc);
            _particleBufferSRVB = new ShaderResourceView(_device, _particleBufferB);
            _particleBufferUAVB = new UnorderedAccessView(_device, _particleBufferB);
        }

        private void InitPressureTexture()
        {
            Texture3DDescription pressureTexDesc = new Texture3DDescription()
            {
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Depth = _numBoxesPerAxis,
                Format = Format.R32_UInt,
                Height = _numBoxesPerAxis,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                Width = _numBoxesPerAxis
            };

            _pressureTexture = new Texture3D(_device, pressureTexDesc);
            _pressureUAV = new UnorderedAccessView(_device, _pressureTexture);
            _pressureSRV = new ShaderResourceView(_device, _pressureTexture);

            pressureTexDesc.Format = Format.R32G32B32A32_Float;

            _pressureGradientTexture = new Texture3D(_device, pressureTexDesc);
            _pressureGradientUAV = new UnorderedAccessView(_device, _pressureGradientTexture);
            _pressureGradientSRV = new ShaderResourceView(_device, _pressureGradientTexture);
        }

        // Do the calculations for the current timestep,
        // update positions and forces of particles
        private void UpdateParticleState()
        {
            _device.ImmediateContext.ComputeShader.Set(_updateParticlePositionsCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessViews(
                new UnorderedAccessView[] { _particleBufferUAVA, _pressureUAV }, 0, 2);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 2);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_pressureGradientSRV, 3);

            _device.ImmediateContext.ComputeShader.SetConstantBuffer(_physicsConstantBuffer, 0);

            _device.ImmediateContext.ClearUnorderedAccessView(_pressureUAV, new int[] { 0, 0, 0, 0 });

            _device.ImmediateContext.Dispatch(_numBoxesTotal, 1, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 2);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 1);
        }

        private void CalculatePressureGradients()
        {
            _device.ImmediateContext.ComputeShader.Set(_calculatePressureGradientCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_pressureGradientUAV, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_pressureSRV, 1);

            _device.ImmediateContext.ComputeShader.SetConstantBuffer(_physicsConstantBuffer, 0);

            _device.ImmediateContext.ClearUnorderedAccessView(_pressureUAV, new float[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_numBoxesPerAxis / 8, _numBoxesPerAxis / 8, _numBoxesPerAxis / 8);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 1);
        }
        private void OutputPressure()
        {
            _device.ImmediateContext.ComputeShader.Set(_pressureOutputCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_renderGridUAV, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_pressureSRV, 1);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_pressureGradientSRV, 3);
            _device.ImmediateContext.ClearUnorderedAccessView(_renderGridUAV, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_numBoxesPerAxis / 8, _numBoxesPerAxis / 8, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 3);
        }

        private void OutputParticlePositions()
        {
            _device.ImmediateContext.ComputeShader.Set(_particlePointOutputCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_renderParticalUAV, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(_particleBufferSRVB, 2);
            _device.ImmediateContext.ClearUnorderedAccessView(_renderParticalUAV, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_numBoxesTotal, 1, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
            _device.ImmediateContext.ComputeShader.SetShaderResource(null, 1);
        }
    }
}
