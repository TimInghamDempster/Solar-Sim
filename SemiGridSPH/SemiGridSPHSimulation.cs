using System.Collections.Generic;
using System.Linq;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDXHelpers;
using SolarSim.GridFluid;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.SemiGridSPH
{
    class SemiGridSPHSimulation : ISimulation
    {
        private readonly OutputShader _outputShader;

        private const int _writeBufferSlot = 0;
        private const int _readBufferSlot = 2;
        private readonly ItemCount<Pixel> _resolution = new ItemCount<Pixel>(256);
        public List<ShaderFileEditor.MarkupTag> MarkupList { get; }

        private readonly List<Vector4> _directions =
            new List<Vector4>
            {
                new Vector4( 1,  0, 0, 0),
                new Vector4(-1,  0, 0, 0),
                new Vector4( 0,  1, 0, 0),
                new Vector4( 0, -1, 0, 0),
                new Vector4( 0,  0, 1, 0),
                new Vector4( 0,  0,-1, 0),
            };


        public SemiGridSPHSimulation(Device device, UnorderedAccessView outputBuffer)
        {
            MarkupList =
                new List<ShaderFileEditor.MarkupTag>()
                {
                    new ShaderFileEditor.MarkupTag("GridReadSlot", _readBufferSlot),
                    new ShaderFileEditor.MarkupTag("GridWriteSlot", _writeBufferSlot),
                    new ShaderFileEditor.MarkupTag("Resolution", _resolution)
                };

            var generatedFilename =
               GenerateTempFile(
                   "GridFluid/GridFluid.hlsl",
                   MarkupList.Concat(
                       OutputShader.MarkupList));

            /*_outputShader =
                new OutputShader(
                    generatedFilename,
                    device,
                    _resolution,
                    outputBuffer,
                    _massVelBuffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _inkBuffers,
                    _inkReadBufferSlot);*/
        }

        public void Dispose()
        {
            _outputShader.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _outputShader.Dispatch();
        }

    }
}
