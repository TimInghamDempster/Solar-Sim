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

    class Grid
    {
        private readonly int _width;
        private readonly int _height;

        public GridPoint[] Points { get; }

        public Grid(int width, int height)
        {
            Points = new GridPoint[width * height];
            _width = width;
            _height = height;

            for (int y = 0; y < height; y++)
            {
                int id = ToGridId(0, y);
            }
        }

        public int ToGridId(int x, int y)
        {
            if (x < 0 || x >= _width ||
                y < 0 || y >= _width)
            {
                return -1;
            }

            return x + y * _width;
        }

        internal void BuildFromHigherRes(Grid higherResGrid, int minifaction)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int id = ToGridId(x, y);
                    float cumulativePressure = 0.0f;
                    Vector3 centreOfPressure = new Vector3(0.0f, 0.0f, 0.0f);

                    for(int dy = 0; dy < minifaction; dy++)
                    {

                        for (int dx = 0; dx < minifaction; dx++)
                        {
                            int higherResId =
                                higherResGrid.ToGridId(
                                    x * minifaction + dx,
                                    y * minifaction + dy);

                            var highResPoint = higherResGrid.Points[higherResId];
                            cumulativePressure += highResPoint.pressure;
                        }
                    }
                    Points[id].pressure = cumulativePressure;
                }
            }
        }
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
        private const int _mipLevels = 5;
        private const int _minificationFactor = 4;
        private const int _firstMinifactionFactor = 2;

        public int RenderWidth => _renderWidth;
        public int RenderHeight => _renderHeight;

        private readonly byte[] _backBuffer = new byte[_renderWidth * _renderHeight * _bytesPerPixel];

        private readonly Grid[] _grids;

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

            _grids = new Grid[_mipLevels];
            InitGrids();

            InitParticles();
        }

        private void InitGrids()
        {
            int currentWidth = _renderWidth;
            int currentHeight = _renderHeight;

            _grids[0] = new Grid(currentWidth, currentHeight);
            currentHeight /= _firstMinifactionFactor;
            currentWidth /= _firstMinifactionFactor;

            for(int i = 1; i < _mipLevels; i++)
            {
                _grids[i] = new Grid(currentWidth, currentHeight);
                currentHeight /= _minificationFactor;
                currentWidth /= _minificationFactor;
            }
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
            CascadePressuresToLowerResolutions();

            ApplyBoundaryConditions();

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

        private void ApplyBoundaryConditions()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                var particle = _particles[i];

                if(particle.Position.X < 0.0f && particle.Velocity.X < 0.0f||
                    particle.Position.X > _renderWidth && particle.Velocity.X > 0.0f)
                {
                    _particles[i].Velocity = new Vector3() { X = particle.Velocity.X * -1.0f, Y = particle.Velocity.Y };
                }

                if (particle.Position.Y < 0.0f && particle.Velocity.Y < 0.0f ||
                    particle.Position.Y > _renderHeight && particle.Velocity.Y > 0.0f)
                {
                    _particles[i].Velocity = new Vector3() { X = particle.Velocity.X, Y = particle.Velocity.Y * -1.0f };
                }
            }
        }

        private void CascadePressuresToLowerResolutions()
        {
            _grids[1].BuildFromHigherRes(_grids[0], _firstMinifactionFactor);

            for (int i = 2; i < _mipLevels; i++)
            {
                _grids[i].BuildFromHigherRes(_grids[i - 1], _minificationFactor);
            }
        }

        private void ClearPressureGrid()
        {
            var gridPoints = _grids.First().Points;
            for (int i = 0; i < gridPoints.Length; i++)
            {
                gridPoints[i].pressure = 0.0f;
            }
        }

        private void PushPressuresToGrid()
        {
            var grid = _grids.First();
            var gridPoints = grid.Points;
            foreach (var particle in _particles)
            {
                foreach (var cornerOffset in _cornerOffsets)
                {
                    var cornerPos = 
                        particle.Position.Floor() + cornerOffset;

                    var delta = particle.Position - cornerPos;
                    var distance = delta.Length;

                    var pressureAtCorner = MathsStuff.LerpClamp(0, 1, distance / MathF.Sqrt(2.0f));

                    var cornerIndex = grid.ToGridId((int)cornerPos.X, (int)cornerPos.Y);
                    if (cornerIndex == -1) continue;

                    gridPoints[cornerIndex].pressure += pressureAtCorner;
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
            var grid = _grids[3];
            var gridPoints = grid.Points;
            const int mask = 255;
            const int scale = 32;
            const double luminanceScale = (256 / scale - 1.0);

            for (int y = 0; y < _renderHeight; y++)
            {
                int bbId = ToBackBufferId(0, y);
                //int gridId = grid.ToGridId(0, y);

                for (int x = 0; x < _renderWidth; x++)
                {
                    int gridId = grid.ToGridId(x / scale, y / scale);
                    int density = (int)(gridPoints[gridId].pressure * luminanceScale);

                    // Clamp within byte size limit, way faster than if-assign
                    density &= mask;

                    _backBuffer[bbId] = (byte)density;
                    _backBuffer[bbId + 3] = 255;

                    // Way faster than calculating every time (like, 20% of frame budget faster)
                    //gridId++;
                    bbId += _bytesPerPixel;
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

        private void ClearBackbuffer()
        {
            Array.Clear(_backBuffer, 0, _backBuffer.Length);
        }
    }
}
