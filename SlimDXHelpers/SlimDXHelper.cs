using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;

namespace SlimDXHelpers
{
    /// <summary>
    /// Various helper functions to make common operations
    /// when wroking with slimDX easier
    /// </summary>
    public class SlimDXHelper
    {
        /// <summary>
        /// Initialise the device and set up a render window
        /// </summary>
        public static RenderForm InitD3D(
            out SlimDX.Direct3D11.Device device, 
            out SwapChain swapChain, 
            out RenderTargetView renderView)
        {
            var form = new RenderForm("Solar simulation");

            form.Width = 1080;
            form.Height = 1080;

            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            SlimDX.Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);

            Factory factory = swapChain.GetParent<Factory>();
            factory.SetWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
            
            RasterizerStateDescription rsd = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                FillMode = FillMode.Solid,
                IsAntialiasedLineEnabled = false,
                IsDepthClipEnabled = false,
                IsFrontCounterclockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = 0.0f
            };

            RasterizerState rs = RasterizerState.FromDescription(device, rsd);
            device.ImmediateContext.Rasterizer.State = rs;


            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(device, backBuffer);

            device.ImmediateContext.OutputMerger.SetTargets(renderView);
            device.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));

            rs.Dispose();
            backBuffer.Dispose();

            return form;
        }
    }
}
