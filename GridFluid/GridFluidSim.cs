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
        private readonly FlipFlop<TextureAndViews> _buffers;

        private const int _writeBufferSlot = 1;
        private const int _readBufferSlot = 2;
        private readonly ItemCount<Pixel> _resolution = new ItemCount<Pixel>(1024);
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

            var pressureTextureA = new TextureAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution);
            var pressureTextureB = new TextureAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution);
            pressureTextureA.FillRandomFloats(MathsAndPhysics.Random);
            pressureTextureB.FillRandomFloats(MathsAndPhysics.Random);

            _buffers = new FlipFlop<TextureAndViews>(pressureTextureA, pressureTextureB);

            TestSetups.CreateDispersionTest(pressureTextureA);

            _outputShader =
                new GridOutputShader(
                    generatedFilename,
                    device,
                    _resolution,
                    outputBuffer,
                    _buffers,
                    _readBufferSlot,
                    _writeBufferSlot);
        }

        public void Dispose()
        {
            _outputShader.Dispose();
            _buffers.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _buffers.Tick();
            _outputShader.Dispatch();
        }

    }
}
