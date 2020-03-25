using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPUTestBed
{
    class Simulation : INotifyPropertyChanged
    {
        public ImageSource BackBufferSource { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private const int _renderWidth = 1024;
        private const int _renderHeight = 1024;
        private const int _bytesPerPixel = 4;
        private const int _backbufferStride = _renderWidth * _bytesPerPixel;

        public int RenderWidth => _renderWidth;
        public int RenderHeight => _renderHeight;

        private readonly byte[] _backBuffer = new byte[_renderWidth * _renderHeight * _bytesPerPixel];
        private readonly Particle[] _particles = new Particle[5000];
        
        public Simulation()
        {
            CompositionTarget.Rendering += (o,e) => RunSimulation();
            var random = new Random();
            for(int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new Particle(random);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RunSimulation()
        {
            ClearBackbuffer();

            UpdateParticles();
            DrawParticles();

            BackBufferSource =
                BitmapSource.Create(
                    _renderWidth,
                    _renderHeight,
                    96, 96,
                    PixelFormats.Bgra32,
                    null,
                    _backBuffer,
                    _backbufferStride);

            OnPropertyChanged(nameof(BackBufferSource));
        }

        private void UpdateParticles()
        {
            foreach (var particle in _particles)
            {
                particle.Position += particle.Velocity;

                if (particle.Position.X >= 1024 && particle.Velocity.X > 0)
                {
                    particle.Velocity.X = -particle.Velocity.X;
                }
                if (particle.Position.Y >= 1024 && particle.Velocity.Y > 0)
                {
                    particle.Velocity.Y = -particle.Velocity.Y;
                }
                if (particle.Position.X < 0 && particle.Velocity.X < 0)
                {
                    particle.Velocity.X = -particle.Velocity.X;
                }
                if (particle.Position.Y < 0 && particle.Velocity.Y < 0)
                {
                    particle.Velocity.Y = -particle.Velocity.Y;
                }
            }
        }

        private void DrawParticles()
        {
            foreach (var particle in _particles)
            {
                int backBufferId = ToBackbufferID((int)particle.Position.X, (int)particle.Position.Y);
                if (backBufferId != -1)
                {
                    _backBuffer[backBufferId + 0] = 255;
                    _backBuffer[backBufferId + 1] = 255;
                    _backBuffer[backBufferId + 2] = 255;
                }
            }
        }

        private int ToBackbufferID(int x, int y)
        {
            if (x >= _renderWidth || y >= RenderHeight ||
                x < 0 || y < 0)
            {
                return -1;
            }

            return x * _bytesPerPixel + y * _renderWidth * _bytesPerPixel;
        }

        private void ClearBackbuffer()
        {
            for (int i = 0; i < _backBuffer.Length; i++)
            {
                _backBuffer[i] = (byte)(i % 4 != 3 ? 0 : 255);
            }
        }
    }
}
