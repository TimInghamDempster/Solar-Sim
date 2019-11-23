using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.HybridFluid
{
    internal class ParticleUpdateShader : AbstractComputeShader
    {
        private readonly ParticleBuffers _particleBuffer;

        public const int ParticleWriteSlot = 0;
        public const int ParticleReadSlot = 2;

        public static List<MarkupTag> MarkupList =>
            new List<MarkupTag>()
            {
                new MarkupTag("UpdateThreads", threadGroupSize),
                new MarkupTag("SimulationUpperBoundary", "1000.0f"),
                new MarkupTag("SimulationLowerBoundary", "0.0f"),
                new MarkupTag("ParticleDefinition", ParticleBuffers.ShaderDefinition),
                new MarkupTag("ParticleReadSlot", ParticleReadSlot),
                new MarkupTag("ParticleWriteSlot", ParticleWriteSlot)
            };

        const int threadGroupSize = 8;

        /// <summary>
        /// Shader which reads in the particles in the read buffer,
        /// updates their state (position, velocity etc), and writes 
        /// them to the output buffer
        /// </summary>
        public ParticleUpdateShader(
            string filename,
            Device device,
            ParticleBuffers particleBuffer,
            int particleCount) :
            base(
                filename,
                "UpdateParticles", 
                device)
        {
            _particleBuffer = particleBuffer ??
                throw new ArgumentNullException(nameof(particleBuffer));

            _threadGroupsX = particleCount / threadGroupSize;
            _threadGroupsY = 1;
            _threadGroupsZ = 1;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);
            _deviceShader.SetUnorderedAccessView(_particleBuffer.WriteBuffer.Object, ParticleWriteSlot);
            _deviceShader.SetShaderResource(_particleBuffer.ReadBuffer.Object, ParticleReadSlot);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, ParticleWriteSlot);
            _deviceShader.SetShaderResource(null, ParticleReadSlot);
            base.PostDispatch(device);
        }
    }
}
