using SlimDX.Direct3D11;
using SlimDXHelpers;
using System.Collections.Generic;
using System.Linq;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.GridFluid
{
    public class GridFluidSim : ISimulation
    {
        private readonly OutputShader _outputShader;
        private readonly TransportShader _transportShader;
        private readonly PressureShader _pressureStep;
        private readonly FlipFlop<Texture3DAndViews> _massVelBuffers;
        private readonly FlipFlop<Texture3DAndViews> _inkBuffers;

        private const int _writeBufferSlot = 0;
        private const int _readBufferSlot = 2;
        private const int _inkWriteBufferSlot = 1;
        private const int _inkReadBufferSlot = 3;
        private readonly ItemCount<Pixel> _resolution = new ItemCount<Pixel>(256);
        public List<MarkupTag> MarkupList { get; }
           

        public GridFluidSim(Device device, UnorderedAccessView outputBuffer)
        {
            MarkupList = 
                new List<MarkupTag>()
                {
                    new MarkupTag("GridReadSlot", _readBufferSlot),
                    new MarkupTag("GridWriteSlot", _writeBufferSlot),
                    new MarkupTag("InkReadSlot", _inkReadBufferSlot),
                    new MarkupTag("InkWriteSlot", _inkWriteBufferSlot),
                    new MarkupTag("Resolution", _resolution)
                };

            var generatedFilename =
               GenerateTempFile(
                   "GridFluid/GridFluid.hlsl",
                   MarkupList.Concat(
                       OutputShader.MarkupList));

            var pressureTextureA = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);
            var pressureTextureB = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);

            _massVelBuffers = new FlipFlop<Texture3DAndViews>(pressureTextureA, pressureTextureB);

            var inkTextureA = new Texture3DAndViews(device, SlimDX.DXGI.Format.R8G8B8A8_SNorm, _resolution, _resolution, _resolution);
            var inkTextureB = new Texture3DAndViews(device, SlimDX.DXGI.Format.R8G8B8A8_SNorm, _resolution, _resolution, _resolution);
            _inkBuffers = new FlipFlop<Texture3DAndViews>(inkTextureA, inkTextureB);

            _outputShader =
                new OutputShader(
                    generatedFilename,
                    device,
                    _resolution,
                    outputBuffer,
                    _massVelBuffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _inkBuffers,
                    _inkReadBufferSlot);

            _transportShader =
                new TransportShader(
                    generatedFilename,
                    device,
                    _massVelBuffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _inkBuffers,
                    _inkReadBufferSlot,
                    _inkWriteBufferSlot,
                    _resolution);

            _pressureStep = 
                new PressureShader(
                    generatedFilename, 
                    device,
                    _massVelBuffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _inkBuffers,
                    _inkWriteBufferSlot,
                    _resolution);
        }

        public void Dispose()
        {
            _outputShader.Dispose();
            _massVelBuffers.Dispose();
            _inkBuffers.Dispose();
            _transportShader.Dispose();
            _pressureStep.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _massVelBuffers.Tick();
            _inkBuffers.Tick();
            _transportShader.Dispatch();
            _massVelBuffers.Tick();
            _pressureStep.Dispatch();
            _outputShader.Dispatch();
        }

    }
}
