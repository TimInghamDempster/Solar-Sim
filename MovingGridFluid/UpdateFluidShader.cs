using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.MovingGridFluid
{
    public class UpdateFluidShader : AbstractComputeShader
    {
        private readonly FlipFlop<Texture3DAndViews> _dataBuffer;
        private readonly int _gridReadSlot;
        private readonly int _gridWriteSlot;
        private const int ThreadGroupSize = 8;

        public UpdateFluidShader(
            string filename,
            Device device,
            FlipFlop<Texture3DAndViews> dataBuffer,
            int gridReadSlot,
            int gridWriteSlot,
            ItemCount<Pixel> gridResolution) :
            base(filename, "UpdateFluid", device)
        {
            _dataBuffer = dataBuffer;
            _gridReadSlot = gridReadSlot;
            _gridWriteSlot = gridWriteSlot;
            _threadGroupsX = gridResolution.Count / ThreadGroupSize;
            _threadGroupsY = gridResolution.Count / ThreadGroupSize;
            _threadGroupsZ = gridResolution.Count / ThreadGroupSize;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);

            _deviceShader.SetUnorderedAccessView(_dataBuffer.WriteObject.UAV, _gridWriteSlot);
            _deviceShader.SetShaderResource(_dataBuffer.ReadObject.SRV, _gridReadSlot);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, _gridWriteSlot);
            _deviceShader.SetShaderResource(null, _gridReadSlot);

            base.PostDispatch(device);
        }
    }
}
