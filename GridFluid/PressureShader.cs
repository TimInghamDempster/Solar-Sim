using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.GridFluid
{
    public class PressureShader : AbstractComputeShader
    {
        private readonly FlipFlop<Texture3DAndViews> _dataBuffer;
        private readonly int _gridReadSlot;
        private readonly int _gridWriteSlot;
        private readonly FlipFlop<Texture3DAndViews> _inkBuffers;
        private readonly int _inkWriteBufferSlot;
        private const int ThreadGroupSize = 8;

        public PressureShader(
            string filename,
            Device device,
            FlipFlop<Texture3DAndViews> dataBuffer,
            int gridReadSlot,
            int gridWriteSlot,
            FlipFlop<Texture3DAndViews> inkBuffers,
            int inkWriteBufferSlot,
            ItemCount<Pixel> gridResolution) :
            base(filename, "PressureStep", device)
        {
            _dataBuffer = dataBuffer;
            _gridReadSlot = gridReadSlot;
            _gridWriteSlot = gridWriteSlot;
            this._inkBuffers = inkBuffers;
            this._inkWriteBufferSlot = inkWriteBufferSlot;
            _threadGroupsX = gridResolution.Count / ThreadGroupSize;
            _threadGroupsY = gridResolution.Count / ThreadGroupSize;
            _threadGroupsZ = gridResolution.Count / ThreadGroupSize;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);

            _deviceShader.SetUnorderedAccessView(_dataBuffer.WriteObject.UAV, _gridWriteSlot);
            _deviceShader.SetShaderResource(_dataBuffer.ReadObject.SRV, _gridReadSlot);
            _deviceShader.SetUnorderedAccessView(_inkBuffers.WriteObject.UAV, _inkWriteBufferSlot);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, _gridWriteSlot);
            _deviceShader.SetShaderResource(null, _gridReadSlot);
            _deviceShader.SetUnorderedAccessView(null, _inkWriteBufferSlot);

            base.PostDispatch(device);
        }
    }
}
