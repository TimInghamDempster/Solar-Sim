using System;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;

namespace Micro_Architecture
{
    class SPHFluidSim : ISimulation
    {
        private Device _device;
        ComputeShader _pressureOutputCS;
        UnorderedAccessView _outputUAV;

        const int _numBoxesPerAxis = 64;
        private int _renderWidth;
        private int _renderHeight;

        public void Init(Device device, UnorderedAccessView outputUAV,int renderWidth, int renderHeight)
        {
            _device = device;
            _outputUAV = outputUAV;
            _pressureOutputCS = BuildComputeShader("OutputPressures");
            _renderHeight = renderHeight;
            _renderWidth = renderWidth;
        }

        public void SimMain()
        {
            OutputPresure();
        }

        private void OutputPresure()
        {
            _device.ImmediateContext.ComputeShader.Set(_pressureOutputCS);
            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(_outputUAV, 0);
            _device.ImmediateContext.ClearUnorderedAccessView(_outputUAV, new[] { 0.0f, 0.0f, 0.0f, 0.0f });

            _device.ImmediateContext.Dispatch(_renderWidth / 8, _renderHeight / 8, 1);

            _device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }

        private ComputeShader BuildComputeShader(string shaderName)
        {
            var csBytecode = ShaderBytecode.CompileFromFile("SPHFluidComputeShaders.hlsl", shaderName, "cs_5_0", ShaderFlags.None, EffectFlags.None);
            var computeShader = new ComputeShader(_device, csBytecode);

            return computeShader;
        }
    }
}
