using SlimDX.Direct3D11;
namespace Micro_Architecture
{
    /// <summary>
    /// Interface that defines a simulation, has functions for initialising and
    /// stepping the simulation.
    /// </summary>
    interface ISimulation
    {
        /// <summary>
        /// Initialise the simulation, will be called once before any simulation timesteps
        /// are performed
        /// </summary>
        /// <param name="device">The d3d11 device</param>
        /// <param name="outputUAV">A uav on a texture which will be drawn to the screen
        /// as the real-time display</param>
        void Init(Device device, UnorderedAccessView outputUAV, int renderWidth, int renderHeight);
        
        /// <summary>
        /// Perform a timestep, is called repeatedly in a loop by the program after initialisation
        /// </summary>
        void SimMain();
    }
}
