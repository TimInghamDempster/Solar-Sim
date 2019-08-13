using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimDXHelpers
{
    public class FlipFlop<ContainedType> : IContext<ContainedType>
    {
        private bool _tickTock;

        private readonly ContainedType _object1;
        private readonly ContainedType _object2;

        public ContainedType Object =>
            _tickTock ? _object1 : _object2;

        /// <summary>
        /// Takes two objects and exposes one of them as a
        /// reference.  Which one is exposed via the property
        /// will change when Tick() is called.
        /// </summary>
        public FlipFlop(
            ContainedType object1,
            ContainedType object2)
        {
            _object1 = object1;

            _object2 = object2;
        }

        public void Tick()
        {
            _tickTock = !_tickTock;
        }
    }
}
