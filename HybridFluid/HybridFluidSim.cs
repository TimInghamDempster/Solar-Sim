using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
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
        private Device _device;

        /// <summary>
        /// Renders the whatever is in its UAV to the
        /// screen, used as the final output stage
        /// </summary>
        private FSQ _finalRender;

        /// <summary>
        /// Used for testing the build process
        /// </summary>
        private TestShader _testShader;

        public HybridFluidSim(FSQ finalRender, Device device)
        {
            _finalRender = finalRender ??
                throw new ArgumentNullException(nameof(finalRender));

            _device = device ??
                throw new ArgumentNullException(nameof(device));

            var generatedFilename =
                GenerateTempFile(
                    "HybridFluid/HybridFluidComputeShaders2.hlsl",
                    new List<MarkupTag>
                    {
                        new MarkupTag("red", "0.1")                        
                    });

            _testShader =
                new TestShader(
                    generatedFilename,
                    "TestShader",
                    _device,
                    _finalRender.UAV);
        }

        public void Dispose()
        {
            _testShader.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _testShader.Dispatch();
        }
    }
}