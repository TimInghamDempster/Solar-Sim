using SlimDX;
using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.GridFluid
{
    public class TransportShader : AbstractComputeShader
    {
        private readonly FlipFlop<Texture3DAndViews> _dataBuffer;
        private readonly int _gridReadSlot;
        private readonly int _gridWriteSlot;
        private readonly FlipFlop<Texture3DAndViews> _inkBuffers;
        private readonly int _inkReadBufferSlot;
        private readonly int _inkWriteBufferSlot;
        private Buffer _dirAndOffset;
        private const int ThreadGroupSize = 8;

        public TransportShader(
            string filename,
            Device device,
            FlipFlop<Texture3DAndViews> dataBuffer,
            int gridReadSlot,
            int gridWriteSlot,
            FlipFlop<Texture3DAndViews> _inkBuffers,
            int _inkReadBufferSlot,
            int _inkWriteBufferSlot,
            ItemCount<Pixel> gridResolution) :
            base(filename, "TransportStep", device)
        {
            _dataBuffer = dataBuffer;
            _gridReadSlot = gridReadSlot;
            _gridWriteSlot = gridWriteSlot;
            this._inkBuffers = _inkBuffers;
            this._inkReadBufferSlot = _inkReadBufferSlot;
            this._inkWriteBufferSlot = _inkWriteBufferSlot;
            _threadGroupsX = gridResolution.Count / ThreadGroupSize;
            _threadGroupsY = gridResolution.Count / ThreadGroupSize;
            _threadGroupsZ = gridResolution.Count / ThreadGroupSize;
            _dirAndOffset = new Buffer(device, 4 * sizeof(int), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 4 * sizeof(int));
        }

        public void SetDirectionAndOffset(Vector4 newDirAndOffset)
        {
            var dataBox = _device.ImmediateContext.MapSubresource(_dirAndOffset, 0, MapMode.WriteDiscard, MapFlags.None);
            dataBox.Data.Write<Vector4>(newDirAndOffset);
            _device.ImmediateContext.UnmapSubresource(_dirAndOffset, 0);
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);

            _deviceShader.SetUnorderedAccessView(_dataBuffer.WriteObject.UAV, _gridWriteSlot);
            _deviceShader.SetShaderResource(_dataBuffer.ReadObject.SRV, _gridReadSlot);

            _deviceShader.SetUnorderedAccessView(_inkBuffers.WriteObject.UAV, _inkWriteBufferSlot);
            _deviceShader.SetShaderResource(_inkBuffers.ReadObject.SRV, _inkReadBufferSlot);

            _deviceShader.SetConstantBuffer(_dirAndOffset, 0);
        }

        protected override void PostDispatch(Device device)
        {
            _deviceShader.SetUnorderedAccessView(null, _gridWriteSlot);
            _deviceShader.SetShaderResource(null, _gridReadSlot);


            _deviceShader.SetUnorderedAccessView(null, _inkWriteBufferSlot);
            _deviceShader.SetShaderResource(null, _inkReadBufferSlot);

            base.PostDispatch(device);
        }
    }
}
