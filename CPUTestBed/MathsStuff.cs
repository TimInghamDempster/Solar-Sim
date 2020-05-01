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
    }
}
