using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.HybridFluid
{
    internal class WriteParticlesToSubspaceShader : AbstractComputeShader
    {
        private readonly SubspaceBuffers _subspaceBuffers;
        private readonly ParticleBuffers _particleBuffers;
        private readonly int _particleReadSlot;
        private const int SubspaceWriteSlot = 3;
        
        public static List<MarkupTag> MarkupList =>
           new List<MarkupTag>()
           {
                new MarkupTag("SubspaceParticleThreads", SubspaceBuffers.ParticlesPerBox),
                new MarkupTag("ParticlesPerBox", SubspaceBuffers.ParticlesPerBox),
                new MarkupTag("SubspaceDefinition", SubspaceBuffers.BoxDefinition),
                new MarkupTag("SubspaceWriteSlot", SubspaceWriteSlot)
           };
        

        public WriteParticlesToSubspaceShader(
            string filename,
            Device device,
            SubspaceBuffers subspaceBuffers,
            ParticleBuffers particleBuffers,
            int particleReadSlot) :
            base(filename, "WriteParticlesToSubspace", device)
        {
            _subspaceBuffers = subspaceBuffers ??
                throw new ArgumentNullException(nameof(subspaceBuffers));

            _particleBuffers = particleBuffers ??
                throw new ArgumentNullException(nameof(particleBuffers));

            _particleReadSlot = particleReadSlot;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);
            _deviceShader.SetShaderResource(_particleBuffers.ReadBuffer.Object, _particleReadSlot);
            _deviceShader.SetUnorderedAccessView(_subspaceBuffers.WriteBuffer, SubspaceWriteSlot);
            _device.ImmediateContext.ClearUnorderedAccessView(_subspaceBuffers.WriteBuffer, new float[] { 0.0f,0.0f,0.0f,0.0f});
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetShaderResource(null, _particleReadSlot);
            _deviceShader.SetUnorderedAccessView(null, SubspaceWriteSlot);
            base.PostDispatch(device);
        }
    }
}
