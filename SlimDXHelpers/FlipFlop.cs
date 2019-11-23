using System;

namespace SlimDXHelpers
{
    public class FlipFlop<ContainedType> : IDisposable
    {
        private bool _tickTock;

        private readonly ContainedType _object1;
        private readonly ContainedType _object2;

        public ContainedType ReadObject =>
            _tickTock ? _object1 : _object2;

        public ContainedType WriteObject =>
            _tickTock ? _object2 : _object1;

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

        public void Dispose()
        {
            if(_object1 is IDisposable disposable1)
            {
                disposable1.Dispose();
            }
            if (_object2 is IDisposable disposable2)
            {
                disposable2.Dispose();
            }
        }
    }
}
