using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPUTestBed.GridBased
{
    struct GridPoint
    {
        public float pressure;
    }
    
    struct Particle
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
    }

    class GridSimulation : INotifyPropertyChanged
    {
        public ImageSource BackBufferSource { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private const int _renderWidth = 1024;
        private const int _renderHeight = 1024;
        private const int _bytesPerPixel = 4;
        private const int _backbufferStride = _renderWidth * _bytesPerPixel;
        private const int _particleCount = 1000;

        public int RenderWidth => _renderWidth;
        public int RenderHeight => _renderHeight;

        private readonly byte[] _backBuffer = new byte[_renderWidth * _renderHeight * _bytesPerPixel];

        GridPoint[] _grid = new GridPoint[_renderWidth + _renderHeight * _renderWidth];
        Particle[] _particles = new Particle[_particleCount];

        private readonly Int2[] _cornerOffsets =
        {
            new Int2(){X = 0, Y = 0},
            new Int2(){X = 1, Y = 0},
            new Int2(){X = 0, Y = 1},
            new Int2(){X = 1, Y = 1},
        };

        private int _framecount;
        private readonly Random _random;

        public GridSimulation()
        {
            CompositionTarget.Rendering += (o,e) => RunSimulation();
            _random = new Random();

            InitParticles();
        }

        private float rnd => (float)_random.NextDouble();

        private void InitParticles()
        {
            const float velConst = 10.0f;
            const float halfCelConst = velConst / 2.0f;
            for(int i = 0; i < _particles.Count(); i++)
            {
                _particles[i].Position = 
                    new Vector3() { X = rnd * _renderWidth, Y = rnd * _renderWidth, Z = 0.0f };
                _particles[i].Velocity =
                    new Vector3() { X = (rnd * velConst) - halfCelConst, Y = (rnd * velConst) - halfCelConst, Z = 0.0f };
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RunSimulation()
        {
            MoveParticles();
            ClearPressureGrid();
            PushPressuresToGrid();

            ClearBackbuffer();
            DrawGrid();
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
            _framecount++;
        }

        private void ClearPressureGrid()
        {
            for(int i = 0; i < _grid.Length; i++)
            {
                _grid[i].pressure = 0.0f;
            }
        }

        private void PushPressuresToGrid()
        {
            foreach (var particle in _particles)
            {
                foreach (var cornerOffset in _cornerOffsets)
                {
                    var cornerPos = 
                        particle.Position.Floor() + cornerOffset;

                    var delta = particle.Position - cornerPos;
                    var distance = delta.Length;

                    var pressureAtCorner = MathsStuff.LerpClamp(0, 1, distance / MathF.Sqrt(2.0f));

                    var cornerIndex = ToGridId((int)cornerPos.X, (int)cornerPos.Y);
                    if (cornerIndex == -1) continue;

                    _grid[cornerIndex].pressure += pressureAtCorner;
                }
            }
        }

        private void MoveParticles()
        {
            for(int i = 0; i < _particles.Count(); i++)
            {
                _particles[i].Position += _particles[i].Velocity;
            }
        }

        private void DrawParticles()
        {
            foreach(var particle in _particles)
            {
                var id = ToBackBufferId((int)particle.Position.X, (int)particle.Position.Y);

                if (id == -1) continue;

                _backBuffer[id + 2] = 255;
            }
        }

        private void DrawGrid()
        {
            for (int y = 0; y < _renderHeight; y++)
            {
                for (int x = 0; x < _renderWidth; x++)
                {
                    int bbId = ToBackBufferId(x, y);
                    int gridId = ToGridId(x, y);

                    int density = (int)(_grid[gridId].pressure * 255);

                    if (density > 255) density = 255;

                    _backBuffer[bbId] = (byte)density;
                }
            }
        }
        
        private int ToBackBufferId(int x, int y)
        {
            if (x < 0 || x >= _renderWidth ||
                y < 0 || y >= _renderWidth)
            {
                return -1;
            }
            return x * _bytesPerPixel + y * _backbufferStride;
        }

        private int ToGridId(int x, int y)
        {
            if(x < 0 || x >= _renderWidth ||
                y < 0 || y >= _renderWidth)
            {
                return -1;
            }

            return x + y * _renderWidth;
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
