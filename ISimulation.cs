using System;

namespace SolarSim
{
    /// <summary>
    /// Interface that defines a simulation, has functions for initialising and
    /// stepping the simulation.
    /// </summary>
    interface ISimulation : IDisposable
    {        
        /// <summary>
        /// Perform a timestep, is called repeatedly in a loop by the program after initialisation
        /// </summary>
        void SimMain(int frameCount);
    }
}
