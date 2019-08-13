using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using System;

namespace SlimDXHelpers
{
    /// <summary>
    /// Inheriting from this will take care of the boilerplate
    /// for running a compute shader, along with some code gen
    /// </summary>
    public abstract class AbstractComputeShader : IDisposable
    {
        protected int _threadGroupsX;
        protected int _threadGroupsY;
        protected int _threadGroupsZ;

        private ComputeShader _computeShader;

        protected readonly Device _device;

        protected ComputeShaderWrapper _deviceShader =>
            _device.ImmediateContext.ComputeShader;

        public AbstractComputeShader(string filename, string shaderName, Device device)
        {
            _device = device ??
                throw new ArgumentNullException(nameof(device));

            var csBytecode = ShaderBytecode.CompileFromFile(filename, shaderName, "cs_5_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization, EffectFlags.None);
            _computeShader = new ComputeShader(_device, csBytecode);

            csBytecode.Dispose();
        }

        /// <summary>
        /// Override this to set up anything specific your compute
        /// shader needs, such as binding resource views
        /// </summary>
        protected virtual void PreviewDispatch(Device device)
        {
            _device.ImmediateContext.ComputeShader.Set(_computeShader);
        }

        /// <summary>
        /// Dispatches the shader
        /// </summary>
        public void Dispatch()
        {
            PreviewDispatch(_device);

            _device.ImmediateContext.Dispatch(_threadGroupsX, _threadGroupsY, _threadGroupsZ);

            PostDispatch(_device);
        }

        // <summary>
        /// Override this to clean up anything specific your compute
        /// shader needs, such as nulling resource views
        /// </summary>
        protected virtual void PostDispatch(Device device) { }        

        public void Dispose()
        {
            _computeShader.Dispose();
        }
    }
}
