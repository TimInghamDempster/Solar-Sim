using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDXHelpers;
using SolarSim.HybridFluid;

namespace SolarSim
{
    class Program
    {
        static int _frameCount = 0;
        static bool _leftDown = false;
        private static SlimDX.Direct3D11.Device _device;
        private static SwapChain _swapChain;
        private static FSQ _finalRender;
        private static RenderTargetView _renderView;

        static ISimulation _simulation;

        static void Main(string[] args)
        {
            // Setup
            var form = SlimDXHelper.InitD3D(out _device, out _swapChain, out _renderView);

            _finalRender = new FSQ(_device, _renderView, "SimpleFSQ.fx");

            _simulation = 
                new HybridFluidSim(
                    _finalRender,
                    _device);

            // Main loop
            MessagePump.Run(form, SimMain);

            // Tear down
            _finalRender.Dispose();

            _swapChain.Dispose();
            _renderView.Dispose();
            _simulation.Dispose();
            _device.Dispose();
        }

        private static void SimMain()
        {
            _simulation.SimMain(_frameCount);
            // Draws the render target to the screen
            _finalRender.Draw();

            _swapChain.Present(0, PresentFlags.None);
            _frameCount++;
        }
    }
}
