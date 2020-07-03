using CPUTestBed.GridBased;
using System;
using System.ComponentModel;
using System.Linq;
using System.Printing.IndexedProperties;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPUTestBed.StretchyGridBased
{
    class GridCell 
    {
        public float Mass { get; set; }
        public float PreviousMass { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 PreviousVelocity { get; set; }
        public float[] Distribution { get; } = new float[9];
        public bool[] ImpassableDirs { get; } = new bool[8];
    }

    struct Rect
    {
        public Rect(float top, float left, float bottom, float right)
        {
            Top = top;
            Left = left;
            Bottom = bottom;
            Right = right;
        }

        public float Area
        {
            get
            {
                float width = Right - Left;
                float height = Bottom - Top;
                return width * height;
            }
        }

        public float Top { get; set; }
        public float Left { get; set; }
        public float Bottom { get; set; }
        public float Right { get; set; }
    }

    class StretchyGrid : INotifyPropertyChanged
    {
        private const int _bytesPerPixel = 4;
        private const int _backbufferStride = _renderWidth * _bytesPerPixel;
        private readonly byte[] _backBuffer = new byte[_renderWidth * _renderHeight * _bytesPerPixel];
        private readonly GridCell[] _grid = new GridCell[_renderWidth * _renderHeight];
        private readonly float _streamVelocity = 0.000007f;
        private readonly float k = 0.01f;
        private const int _border = 5;
        private int _framecount;

        private Int2[] _directions =
        {
            new Int2() {X = -1, Y = -1},
            new Int2() {X =  0, Y = -1},
            new Int2() {X =  1, Y = -1},

            new Int2() {X = -1, Y =  0},
            new Int2() {X =  1, Y =  0},

            new Int2() {X = -1, Y =  1},
            new Int2() {X =  0, Y =  1},
            new Int2() {X =  1, Y =  1},
        };

        private Int2[] _sampleDirs = new Int2[_border * _border - 1];

        private Int2[] _invDirections =
        {
            new Int2() {X =  1, Y =  1},
            new Int2() {X =  0, Y =  1},
            new Int2() {X = -1, Y =  1},

            new Int2() {X =  1, Y =  0},
            new Int2() {X = -1, Y =  0},

            new Int2() {X =  1, Y = -1},
            new Int2() {X =  0, Y = -1},
            new Int2() {X = -1, Y = -1},
        };

        public StretchyGrid()
        {
            CompositionTarget.Rendering += (o, e) => RunSimulation();

            int sampleId = 0;
            var halfBorder = _border / 2;
            for(int y = -halfBorder; y < halfBorder + 1; y++)
            {
                for(int x = -halfBorder; x < halfBorder + 1; x++)
                {
                    if (x == 0 && y == 0) continue;

                    _sampleDirs[sampleId] = new Int2() { X = x, Y = y };
                    sampleId++;
                }    
            }    

            for (int y = 0; y < _renderHeight; y++)
            {
                for (int x = 0; x < _renderWidth; x++)
                {
                    int gridId = ToGridId(x, y);
                    _grid[gridId] = new GridCell();

                    for(int i = 0; i < _directions.Length; i++)
                    {
                        var overallX = x + _directions[i].X;
                        var overallY = y + _directions[i].Y;

                        if(overallX > 32 && overallX < 48 &&
                            overallY > 120 && overallY < 136)
                        {
                            _grid[gridId].ImpassableDirs[i] = true;
                        }
                        if(overallY == 0 || overallY == _renderHeight - 1)
                        {
                            _grid[gridId].ImpassableDirs[i] = true;
                        }
                    }

                    _grid[gridId].Mass = 0.5f;
                    _grid[gridId].Velocity = new Vector3(_streamVelocity, 0.0f, 0.0f);
                }
            }

            ApplyBoundary();
        }

        public ImageSource BackBufferSource { get; private set; }

        private void RunSimulation()
        {
            ApplyBoundary();
            DistributeFluid();
            MoveFluid();

            ClearBackbuffer();
            DrawGrid();

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
            Thread.Sleep(1);
            _framecount++;
        }

        private void ApplyBoundary()
        {
            for (int y = 0; y < _renderHeight; y++)
            {
                int gridId = ToGridId(5, y);
                _grid[gridId].Mass = 0.5f;
                _grid[gridId].Velocity = new Vector3(_streamVelocity, 0.0f, 0.0f);

                int lastBoundary = _renderWidth - 5;
                gridId = ToGridId(lastBoundary, y);
                _grid[gridId].Mass = 0.5f;
                _grid[gridId].Velocity = new Vector3(_streamVelocity, 0.0f, 0.0f);
            }
        }

        private void MoveFluid()
        {
            Parallel.For(_border, _renderWidth - _border, (y) =>
            {
                int gridId = ToGridId(_border, y);

                for (int x = _border; x < _renderWidth - _border; x++)
                {
                    var cell = _grid[gridId];
                    float mass = cell.PreviousMass * cell.Distribution[8];
                    Vector3 momentum = cell.PreviousVelocity * mass;

                    if (cell.Mass != 0.0f)
                    {
                        int temp = 0;
                    }

                    for (int i = 0; i < _invDirections.Length; i++)
                    {
                        var dir = _invDirections[i];
                        var altGrid = ToGridId(x + dir.X, y + dir.Y);
                        var otherCell = _grid[altGrid];

                        var transferringMass = otherCell.PreviousMass * otherCell.Distribution[i];
                        mass += transferringMass;
                        momentum += otherCell.PreviousVelocity * transferringMass;
                    }

                    cell.Mass = mass;
                    if (mass != 0.0f)
                    {
                        cell.Velocity = momentum / mass;
                    }

                    // Way faster than calculating every time (like, 20% of frame budget faster)
                    gridId++;
                }
            });
        }

        private void DistributeFluid()
        {
            var directionCount = _directions.Length;
            Parallel.For(_border, _renderHeight - _border, (y) =>
            {
                int gridId = ToGridId(_border, y);

                for (int x = _border; x < _renderWidth - _border; x++)
                {
                    var cell = _grid[gridId];
                    Rect stretchedCell = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

                    if (cell.Mass != 0.0f)
                    {
                        int temp = 0;
                    }

                    stretchedCell.Left += cell.Velocity.X;
                    stretchedCell.Right += cell.Velocity.X;
                    stretchedCell.Top += cell.Velocity.Y;
                    stretchedCell.Bottom += cell.Velocity.Y;

                    for (int i = 0; i < directionCount; i++)
                    {
                        if (cell.ImpassableDirs[i]) continue;

                        var dir = _directions[i];
                        var testRect = new Rect(
                            dir.Y,
                            dir.X,
                            dir.Y + 1.0f,
                            dir.X + 1.0f);

                        var overlapArea = CalcOverlap(testRect, stretchedCell);
                        cell.Distribution[i] = overlapArea / stretchedCell.Area;
                    }

                    foreach(var dir in _sampleDirs)
                    {
                        var otherCellId = ToGridId(x + dir.X, y + dir.Y);
                        var otherCell = _grid[otherCellId];
                        var pressureDiff = (cell.Mass - otherCell.Mass) / ((dir.X * dir.X) + (dir.Y * dir.Y));

                        cell.Velocity += new Vector3(dir.X, dir.Y, 0) * pressureDiff * k;
                    }

                    // Clear old value so we don't skew the total
                    // outlfow calculation
                    cell.Distribution[8] = 0.0f;

                    var amountDistributed = cell.Distribution.Sum();

                    cell.Distribution[8] = 1.0f - amountDistributed;
                    cell.PreviousMass = cell.Mass;
                    cell.PreviousVelocity = cell.Velocity;

                    // Way faster than calculating every time (like, 20% of frame budget faster)
                    gridId++;
                }
            });
        }

        private float CalcOverlap(Rect rect1, Rect rect2)
        {
            var overlapRect = new Rect(
                Max(rect1.Top, rect2.Top),
                Max(rect1.Left, rect2.Left),
                Min(rect1.Bottom, rect2.Bottom),
                Min(rect1.Right, rect2.Right));

            if (overlapRect.Top > overlapRect.Bottom) return 0.0f;
            if (overlapRect.Left > overlapRect.Right) return 0.0f;

            return overlapRect.Area;
        }

        private void DrawGrid()
        {
            for (int y = 0; y < _renderHeight; y++)
            {
                int bbId = ToBackBufferId(0, y);
                int gridId = ToGridId(0, y);

                for (int x = 0; x < _renderWidth; x++)
                {
                    var val = (int)(255.0f * _grid[gridId].Mass);
                    if (val > 255) val = 255;
                    _backBuffer[bbId] = (byte)val;
                    _backBuffer[bbId + 3] = 255;

                    // Way faster than calculating every time (like, 20% of frame budget faster)
                    bbId += _bytesPerPixel;
                    gridId++;
                }
            }
        }

        private int ToGridId(int x, int y)
        {
            return x  + y * _renderWidth;
        }

        private float Min(float a, float b)
        {
            return a < b ? a : b;
        }
        private float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        private int ToBackBufferId(int x, int y)
        {
            return x * _bytesPerPixel + y * _backbufferStride;
        }

        private void ClearBackbuffer()
        {
            Array.Clear(_backBuffer, 0, _backBuffer.Length);
        }

        private const int _renderWidth = 256;
        public int RenderWidth => _renderWidth;
        private const int _renderHeight = 256;
        public int RenderHeight => _renderHeight;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
