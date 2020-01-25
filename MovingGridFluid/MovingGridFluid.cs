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
        private readonly int _velReadBufferSlot = 3;
        private readonly int _velWriteBufferSlot = 1;
        private readonly UpdateFluidShader _updateFluidShader;
        private readonly InitialiseFluidShader _initialiseShader;
        private readonly FlipFlop<Texture3DAndViews> _massPosBuffers;
        private readonly FlipFlop<Texture3DAndViews> _velocityBuffers;

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
                    new MarkupTag("VelocityGridReadSlot", _velReadBufferSlot),
                    new MarkupTag("VelocityGridWriteSlot", _velWriteBufferSlot),
                    new MarkupTag("Resolution", _resolution),
                    new MarkupTag("ObsPos", 64.0f)
                 };

            var pressureTextureA = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);
            var pressureTextureB = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);

            _massPosBuffers = new FlipFlop<Texture3DAndViews>(pressureTextureA, pressureTextureB);

            var velTextureA = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);
            var velTextureB = new Texture3DAndViews(device, SlimDX.DXGI.Format.R32G32B32A32_Float, _resolution, _resolution, _resolution);

            _velocityBuffers = new FlipFlop<Texture3DAndViews>(velTextureA, velTextureB);

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
                    _resolution,
                    _velocityBuffers,
                    _velReadBufferSlot,
                    _velWriteBufferSlot);

            _initialiseShader =
                new InitialiseFluidShader(
                    generatedFilename,
                    device,
                    _massPosBuffers,
                    _readBufferSlot,
                    _writeBufferSlot,
                    _resolution,
                    _velocityBuffers,
                    _velReadBufferSlot,
                    _velWriteBufferSlot);

            _initialiseShader.Dispatch();
            _velocityBuffers.Tick();
            _massPosBuffers.Tick();
            _initialiseShader.Dispatch();
        }

        public void Dispose()
        {
            _initialiseShader.Dispose();
            _updateFluidShader.Dispose();
            _massPosBuffers.Dispose();
            _velocityBuffers.Dispose();
            _outputShader.Dispose();
        }

        public void SimMain(int frameCount)
        {
            _massPosBuffers.Tick();
            _velocityBuffers.Tick();
            _updateFluidShader.Dispatch();
            _outputShader.Dispatch();
        }
    }
}
