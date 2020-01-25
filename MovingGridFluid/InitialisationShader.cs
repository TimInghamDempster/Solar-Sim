using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.MovingGridFluid
{
    public class InitialiseFluidShader : AbstractComputeShader
    {
        private readonly FlipFlop<Texture3DAndViews> _dataBuffer;
        private readonly int _gridReadSlot;
        private readonly int _gridWriteSlot;
        private readonly FlipFlop<Texture3DAndViews> _velocityBuffer;
        private readonly int _velocityGridReadSlot;
        private readonly int _velocityGridWriteSlot;
        private const int ThreadGroupSize = 8;

        public InitialiseFluidShader(
            string filename,
            Device device,
            FlipFlop<Texture3DAndViews> dataBuffer,
            int gridReadSlot,
            int gridWriteSlot,
            ItemCount<Pixel> gridResolution,
            FlipFlop<Texture3DAndViews> velocityBuffer,
            int velocityGridReadSlot,
            int velocityGridWriteSlot) :
            base(filename, "InitialiseFluid", device)
        {
            _dataBuffer = dataBuffer;
            _gridReadSlot = gridReadSlot;
            _gridWriteSlot = gridWriteSlot;
            _velocityBuffer = velocityBuffer;
            _velocityGridReadSlot = velocityGridReadSlot;
            _velocityGridWriteSlot = velocityGridWriteSlot;
            _threadGroupsX = gridResolution.Count / ThreadGroupSize;
            _threadGroupsY = gridResolution.Count / ThreadGroupSize;
            _threadGroupsZ = gridResolution.Count / ThreadGroupSize;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);

            _deviceShader.SetUnorderedAccessView(_dataBuffer.WriteObject.UAV, _gridWriteSlot);
            _deviceShader.SetShaderResource(_dataBuffer.ReadObject.SRV, _gridReadSlot);
            _deviceShader.SetUnorderedAccessView(_velocityBuffer.WriteObject.UAV, _velocityGridWriteSlot);
            _deviceShader.SetShaderResource(_velocityBuffer.ReadObject.SRV, _velocityGridReadSlot);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, _gridWriteSlot);
            _deviceShader.SetShaderResource(null, _gridReadSlot);
            _deviceShader.SetUnorderedAccessView(null, _velocityGridWriteSlot);
            _deviceShader.SetShaderResource(null, _velocityGridReadSlot);

            base.PostDispatch(device);
        }
    }
}
