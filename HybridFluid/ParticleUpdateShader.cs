using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.HybridFluid
{
    internal class ParticleUpdateShader : AbstractComputeShader
    {
        ParticleBuffers _particleBuffer;

        public static List<MarkupTag> MarkupList =>
            new List<MarkupTag>()
            {
                new MarkupTag("UpdateThreads", threadGroupSize.ToString()),
                new MarkupTag("SimulationUpperBoundary", "1000.0f"),
                new MarkupTag("SimulationLowerBoundary", "0.0f"),
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
            _deviceShader.SetUnorderedAccessView(_particleBuffer.WriteBuffer.Object, 0);
            _deviceShader.SetShaderResource(_particleBuffer.ReadBuffer.Object, 2);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, 0);
            _deviceShader.SetShaderResource(null, 2);
            base.PostDispatch(device);
        }
    }
}
