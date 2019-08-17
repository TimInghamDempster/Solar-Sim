using SlimDX;
using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.HybridFluid
{
    /// <summary>
    /// A hybrid approach using a fixed number of sph particles
    /// in each grid cell, scaled by the number of particles actually
    /// in the cell
    /// </summary>
    internal class HybridFluidSim : ISimulation
    {
        private readonly Device _device;

        /// <summary>
        /// Renders the whatever is in its UAV to the
        /// screen, used as the final output stage
        /// </summary>
        private readonly FSQ _finalRender;

        /// <summary>
        /// Dumps the particles into the output buffer
        /// </summary>
        private readonly ParticleOutputShader _particleOutputShader;

        /// <summary>
        /// Update particle positions, simple constant velocity
        /// update for now
        /// </summary>
        private readonly ParticleUpdateShader _particleUpdateShader;

        /// <summary>
        /// Stores the particle data.  Double buffered and
        /// flip-flops between the buffers
        /// </summary>
        private readonly ParticleBuffers _particleBuffers;

        /// <summary>
        /// A fluid simulation using a hybrid SPH and grid
        /// based method.  Each SPH particle is calculated
        /// against a fixed number of other particles, with
        /// the results scaled to account for the particles
        /// which are not tested against
        /// </summary>
        public HybridFluidSim(FSQ finalRender, Device device)
        {
            _finalRender = finalRender ??
                throw new ArgumentNullException(nameof(finalRender));

            _device = device ??
                throw new ArgumentNullException(nameof(device));

            var generatedFilename =
                GenerateTempFile(
                    "HybridFluid/HybridFluidComputeShaders2.hlsl",
                    ParticleOutputShader.MarkupList.
                    Concat(ParticleUpdateShader.MarkupList));

            const float fieldHalfSize = 500.0f;
            const int particleCount = 10000;

            _particleBuffers =
                new ParticleBuffers(
                    _device,
                    //new Vector3(fieldHalfSize / 3.0f, fieldHalfSize, 0.0f),
                    //new Vector3(fieldHalfSize * 1.6f, fieldHalfSize, fieldHalfSize),
                    new Vector3(fieldHalfSize, fieldHalfSize, 0.0f),
                    new Vector3(fieldHalfSize, fieldHalfSize, fieldHalfSize),
                    particleCount);

            _particleUpdateShader =
                new ParticleUpdateShader(
                    generatedFilename,
                    _device,
                    _particleBuffers,
                    particleCount);

            _particleOutputShader = 
                new ParticleOutputShader(
                    generatedFilename,
                    _device,
                    _particleBuffers.ReadBuffer,
                    _finalRender.UAV,
                    particleCount);
        }

        public void Dispose()
        {
            _particleOutputShader.Dispose();
            _particleUpdateShader.Dispose();
            _particleBuffers.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _particleUpdateShader.Dispatch();

            _particleOutputShader.Dispatch();

            _particleBuffers.Tick();
        }
    }
}