using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.HybridFluid
{
    internal class ParticleOutputShader : AbstractComputeShader
    {
        private readonly IContext<ShaderResourceView> _particleBuffer;
        private readonly UnorderedAccessView _outputBuffer;

        public static List<MarkupTag> MarkupList =>
            new List<MarkupTag>()
            {
                new MarkupTag("OutputThreads", ThreadGroupSize)
            };

        public const int ThreadGroupSize = 8;

        /// <summary>
        /// A shader which takes the current read particle buffer and
        /// draws the particles in it to the output screen buffer
        /// </summary>
        public ParticleOutputShader(
            string filename,
            Device device,
            IContext<ShaderResourceView> particleBuffer,
            UnorderedAccessView outputBuffer,
            int particleCount) :
            base(
                filename,
                "OutputParticles", 
                device)
        {
            _particleBuffer = particleBuffer ??
                throw new ArgumentNullException(nameof(particleBuffer));

            _outputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));

            _threadGroupsX = particleCount / ThreadGroupSize;
            _threadGroupsY = 1;
            _threadGroupsZ = 1;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);
            _deviceShader.SetUnorderedAccessView(_outputBuffer, 0);

            _device.
                ImmediateContext.
                ClearUnorderedAccessView(
                    _outputBuffer, 
                    new float[] { 0.0f,0.0f,0.0f,0.0f});

            _deviceShader.SetShaderResource(_particleBuffer.Object, 2);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, 0);
            _deviceShader.SetShaderResource(null, 2);
            base.PostDispatch(device);
        }
    }
}
