using SlimDX.Direct3D11;
using SlimDXHelpers;

namespace SolarSim.HybridFluid
{
    /// <summary>
    /// A shader used to test the code gen and shader loading
    /// and running code
    /// </summary>
    internal class TestShader : AbstractComputeShader
    {
        UnorderedAccessView _outputUAV;
        public TestShader(
            string filename, 
            string shaderName,
            Device device,
            UnorderedAccessView outputUAV) :
            base(
                filename,
                shaderName, 
                device)
        {
            _outputUAV = outputUAV;
            _threadGroupsX = 100;
            _threadGroupsY = 100;
            _threadGroupsZ = 100;
        }

        protected override void PreviewDispatch(Device device)
        {
            base.PreviewDispatch(device);
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_outputUAV, 0);
        }

        protected override void PostDispatch(Device device)
        {
            base.PostDispatch(device);

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }
    }
}
