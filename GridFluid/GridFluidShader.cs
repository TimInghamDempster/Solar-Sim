using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.GridFluid
{
    /// <summary>
    /// Each cell needs to figure out how much fluid is trying
    /// to flow out of it as this is a sum over several faces
    /// which can add up to more fluid than there is.  When
    /// performing the fluid transport step the flows need
    /// to use this value as they might need scaling to avoid
    /// outflowing more fluid than exists in a given cell
    /// </summary>
    public class GridFluidShader : AbstractComputeShader
    {
        private readonly FlipFlop<Texture3DAndViews> _dataBuffer;
        private readonly int _gridReadSlot;
        private readonly int _gridWriteSlot;
        private const int ThreadGroupSize = 8;

        public GridFluidShader(
            string filename,
            Device device,
            FlipFlop<Texture3DAndViews> dataBuffer,
            int gridReadSlot,
            int gridWriteSlot,
            ItemCount<Pixel> gridResolution) :
            base(filename, "GridFluidMain", device)
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
