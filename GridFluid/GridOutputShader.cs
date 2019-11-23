using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.GridFluid
{
    public class GridOutputShader : AbstractComputeShader
    {
        private readonly UnorderedAccessView _outputBuffer;
        private readonly FlipFlop<TextureAndViews> _dataBuffer;
        private readonly int _gridReadSlot;
        private readonly int _gridWriteSlot;

        public static List<MarkupTag> MarkupList =>
            new List<MarkupTag>()
            {
                new MarkupTag("OutputThreads", ThreadGroupSize),
                new MarkupTag("OutputSlot", FinalOutputSlot)
            };

        public const int ThreadGroupSize = 8;
        public const int FinalOutputSlot = 0;

        public GridOutputShader(
            string filename,
            Device device, 
            ItemCount<Pixel> outputResolution,
            UnorderedAccessView outputBuffer,
            FlipFlop<TextureAndViews> dataBuffer,
            int gridReadSlot,
            int gridWriteSlot) :
            base(filename, "OutputGrid", device)
        {
            _outputBuffer = outputBuffer ??
               throw new ArgumentNullException(nameof(outputBuffer));
            _gridReadSlot = gridReadSlot;
            _gridWriteSlot = gridWriteSlot;
            _threadGroupsX = outputResolution.Count / ThreadGroupSize;
            _threadGroupsY = outputResolution.Count / ThreadGroupSize;
            _threadGroupsZ = 1;
            _dataBuffer = dataBuffer;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);

            _deviceShader.SetUnorderedAccessView(_outputBuffer, FinalOutputSlot);

            _device.
                ImmediateContext.
                ClearUnorderedAccessView(
                    _outputBuffer,
                    new float[] { 0.0f, 1.0f, 0.0f, 0.0f });

            _deviceShader.SetShaderResource(_dataBuffer.ReadObject.SRV, _gridReadSlot);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, FinalOutputSlot);
            _deviceShader.SetShaderResource(null, _gridReadSlot);

            base.PostDispatch(device);
        }
    }
}
