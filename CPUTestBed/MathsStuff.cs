using System;
using System.Collections.Generic;
using System.Text;

namespace CPUTestBed
{
    class MathsStuff
    {
        public static float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        /// <summary>
        /// Lerps but clamps the lerpVal between 0 and 1
        /// </summary>
        public static float LerpClamp(float firstFloat, float secondFloat, float by)
        {
            if (by > 1.0f) by = 1.0f;
            if (by < 0.0f) by = 0.0f;

            return Lerp(firstFloat, secondFloat, by);
        }
    }
}
