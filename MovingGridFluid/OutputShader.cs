using SlimDX.Direct3D11;
using SlimDXHelpers;
using System;
using System.Collections.Generic;
using static SlimDXHelpers.ShaderFileEditor;

namespace SolarSim.MovingGridFluid
{
    public class OutputShader : AbstractComputeShader
    {
        private readonly UnorderedAccessView _outputBuffer;
        private readonly FlipFlop<Texture3DAndViews> _dataBuffer;
        private readonly FlipFlop<Texture3DAndViews> _veloctiyBuffer;
        private readonly int _velocityReadSlot;
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

        public OutputShader(
            string filename,
            Device device, 
            ItemCount<Pixel> outputResolution,
            UnorderedAccessView outputBuffer,
            FlipFlop<Texture3DAndViews> dataBuffer,
            FlipFlop<Texture3DAndViews> velocityBuffer,
            int velocityReadSlot,
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
            _veloctiyBuffer = velocityBuffer;
            _velocityReadSlot = velocityReadSlot;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);

            _deviceShader.SetUnorderedAccessView(_outputBuffer, FinalOutputSlot);

            _device.
                ImmediateContext.
                ClearUnorderedAccessView(
                    _outputBuffer,
                    new float[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _deviceShader.SetShaderResource(_dataBuffer.ReadObject.SRV, _gridReadSlot);
            _deviceShader.SetShaderResource(_veloctiyBuffer.ReadObject.SRV, _velocityReadSlot);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, FinalOutputSlot);
            _deviceShader.SetShaderResource(null, _gridReadSlot);

            base.PostDispatch(device);
        }
    }
}
