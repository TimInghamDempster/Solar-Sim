using SlimDX;
using SlimDXHelpers;

namespace SolarSim.GridFluid
{
    public static class TestSetups
    {
        /// <summary>
        /// Create initial conditions of a single fluid filled pixel surrounded
        /// by vacuum to test dispersion simulation
        /// </summary>
        public static void CreateDispersionTest(Texture2DAndViews initialBuffer)
        {
            var stream = new DataStream(initialBuffer.ResourceSize.RequiredBytes, true, true);

            // two blank rows
            for (int rowId = 0; rowId < 2; rowId++)
            {
                for (int index = 0; index < initialBuffer.Width.Count; index++)
                {
                    stream.Write(0.0f);
                    stream.Write(0.0f);
                    stream.Write(0.0f);
                    stream.Write(0.0f);
                }
            }

            // two blank pixels
            for (int rowId = 0; rowId < 2; rowId++)
            {
                stream.Write(0.0f);
                stream.Write(0.0f);
                stream.Write(0.0f);
                stream.Write(0.0f);
            }
            // full pressure pixel
            stream.Write(1.0f);
            stream.Write(0.0f);
            stream.Write(0.0f);
            stream.Write(0.0f);
            // rest of row 3 is blank
            for (int index = 3; index < initialBuffer.Width.Count; index++)
            {
                stream.Write(0.0f);
                stream.Write(0.0f);
                stream.Write(0.0f);
                stream.Write(0.0f);
            }

            // rest of the texture is blank
            for (int rowId = 3; rowId < initialBuffer.Height.Count; rowId++)
            {
                for (int index = 0; index < initialBuffer.Width.Count; index++)
                {
                    stream.Write(0.0f);
                    stream.Write(0.0f);
                    stream.Write(0.0f);
                    stream.Write(0.0f);
                }
            }

            initialBuffer.FillWithData(stream);
        }
    }
}
