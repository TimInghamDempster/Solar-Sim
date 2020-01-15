using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.MovingGridFluid
{
    public class MovingGridFluid : ISimulation
    {
        private readonly Device _device;
        private readonly UnorderedAccessView _outputBuffer;

        public List<MarkupTag> MarkupList { get; }

        private readonly OutputShader _outputShader;
        private readonly ItemCount<Pixel> _resolution = new ItemCount<Pixel>(256);
        private readonly int _readBufferSlot = 2;
        private readonly int _writeBufferSlot = 0;
        private readonly UpdateFluidShader _updateFluidShader;
        private readonly FlipFlop<Texture3DAndViews> _massPosBuffers;

        public MovingGridFluid(
            Device device, 
            UnorderedAccessView outputBuffer)
        {
            _device = device;
            _outputBuffer = outputBuffer;

            MarkupList =
                 new List<MarkupTag>()
                 {
                    new MarkupTag("GridReadSlot", _readBufferSlot),
                    new MarkupTag("GridWriteSlot", _writeBufferSlot),
                    new MarkupTag("Resolution", _resolution)
                 };

            var pressureTextureA = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);
            var pressureTextureB = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);

            _massPosBuffers = new FlipFlop<Texture3DAndViews>(pressureTextureA, pressureTextureB);

            var generatedFilename =
               GenerateTempFile(
                   "MovingGridFluid/MovingGridFluid.hlsl",
                   MarkupList.Concat(
                       OutputShader.MarkupList));

            _outputShader =
                new OutputShader(
                    generatedFilename,
                    device,
                    _resolution,
                    outputBuffer,
                    _massPosBuffers,
                    _readBufferSlot,
                    _writeBufferSlot);

            _updateFluidShader =
                new UpdateFluidShader(
                    generatedFilename,
                    device,
                    _massPosBuffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _resolution);
        }

        public void Dispose()
        {
            _updateFluidShader.Dispose();
            _massPosBuffers.Dispose();
            _outputShader.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _massPosBuffers.Tick();
            _updateFluidShader.Dispatch();
            _outputShader.Dispatch();
        }
    }
}
