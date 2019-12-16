using SlimDXHelpers;

namespace SolarSim.GridFluid
{
    public class GridTextureSet
    {
        public Texture2DAndViews PressureVelocity { get; }

        public Texture2DAndViews InkColours { get; }

        public GridTextureSet(
            Texture2DAndViews pressureVeolcity,
            Texture2DAndViews inkColours)
        {
            PressureVelocity = pressureVeolcity;
            InkColours = inkColours;
        }
    }
}
