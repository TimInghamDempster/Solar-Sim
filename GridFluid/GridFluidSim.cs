using SlimDX.Direct3D11;
using SlimDXHelpers;
using System.Collections.Generic;
using System.Linq;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.GridFluid
{
    public class GridFluidSim : ISimulation
    {
        private readonly GridOutputShader _outputShader;
        private readonly GridFluidShader _transportStep;
        private readonly FlipFlop<Texture3DAndViews> _buffers;

        private const int _writeBufferSlot = 0;
        private const int _readBufferSlot = 2;
        private readonly ItemCount<Pixel> _resolution = new ItemCount<Pixel>(256);
        public List<MarkupTag> MarkupList { get; }
           

        public GridFluidSim(Device device, UnorderedAccessView outputBuffer)
        {
            MarkupList = 
                new List<MarkupTag>()
                {
                    new MarkupTag("GridReadSlot", _readBufferSlot),
                    new MarkupTag("GridWriteSlot", _writeBufferSlot),
                    new MarkupTag("Resolution", _resolution)
                };

            var generatedFilename =
               GenerateTempFile(
                   "GridFluid/GridFluid.hlsl",
                   MarkupList.Concat(
                       GridOutputShader.MarkupList));

            var pressureTextureA = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);
            var pressureTextureB = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);

            _buffers = new FlipFlop<Texture3DAndViews>(pressureTextureA, pressureTextureB);

            _outputShader =
                new GridOutputShader(
                    generatedFilename,
                    device,
                    _resolution,
                    outputBuffer,
                    _buffers,
                    _readBufferSlot,
                    _writeBufferSlot);

            _transportStep = 
                new GridFluidShader(
                    generatedFilename, 
                    device,
                    _buffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _resolution);
        }

        public void Dispose()
        {
            _outputShader.Dispose();
            _buffers.Dispose();
            _transportStep.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _buffers.Tick();
            _transportStep.Dispatch();
            _outputShader.Dispatch();
        }

    }
}
