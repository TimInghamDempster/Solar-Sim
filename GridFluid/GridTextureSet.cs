using SlimDXHelpers;

namespace SolarSim.GridFluid
{
    public class GridTextureSet
    {
        public TextureAndViews PressureVelocity { get; }

        public TextureAndViews InkColours { get; }

        public GridTextureSet(
            TextureAndViews pressureVeolcity,
            TextureAndViews inkColours)
        {
            PressureVelocity = pressureVeolcity;
            InkColours = inkColours;
        }
    }
}
