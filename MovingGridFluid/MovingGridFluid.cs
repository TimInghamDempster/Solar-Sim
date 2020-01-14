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

        public MovingGridFluid(
            Device device, 
            UnorderedAccessView outputBuffer)
        {
            _device = device;
            _outputBuffer = outputBuffer;

            MarkupList =
                 new List<MarkupTag>()
                 {
                    //new MarkupTag("GridReadSlot", _readBufferSlot),
                   // new MarkupTag("GridWriteSlot", _writeBufferSlot),
                   // new MarkupTag("InkReadSlot", _inkReadBufferSlot),
                   // new MarkupTag("InkWriteSlot", _inkWriteBufferSlot),
                    new MarkupTag("Resolution", _resolution)
                 };

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
                    //_massVelBuffers,
                    0,//_readBufferSlot,
                    0,//_writeBufferSlot,
                      //_inkBuffers,
                    0);//_inkReadBufferSlot);
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
