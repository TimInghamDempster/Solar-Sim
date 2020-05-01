using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private const int _boxesPerAxis = 32;

        public int RenderWidth => _renderWidth;
        public int RenderHeight => _renderHeight;

        private readonly byte[] _backBuffer = new byte[_renderWidth * _renderHeight * _bytesPerPixel];
        private readonly Box[] _boxesA = new Box[_boxesPerAxis * _boxesPerAxis];
        private readonly Box[] _boxesB = new Box[_boxesPerAxis * _boxesPerAxis];

        private Box[] _currentBoxes;
        private Box[] _previousBoxes;
        private int _framecount;

        private readonly Box _nullBox = new Box(new Random(), new Vector3(), new Vector3(), 0, new Particle(new Random(), 0,0,0));
        private readonly Particle _nullParticle = new Particle(new Random(), 0, 0, 0);

        private readonly Int2[] _directions = new[]
        {
            new Int2() {X= -1, Y =  1},
            new Int2() {X=  0, Y =  1},
            new Int2() {X=  1, Y =  1},
            new Int2() {X= -1, Y =  0},
            new Int2() {X=  0, Y =  0},
            new Int2() {X=  1, Y =  0},
            new Int2() {X= -1, Y = -1},
            new Int2() {X=  0, Y = -1},
            new Int2() {X=  1, Y = -1},
        };
        private readonly Random _random;

        public Simulation()
        {
            CompositionTarget.Rendering += (o,e) => RunSimulation();
            _random = new Random();
            _nullParticle.Velocity = new Vector3();

            float boxSize = _renderWidth / _boxesPerAxis;

            for (int x = 0; x < _boxesPerAxis; x++)
            {
                for (int y = 0; y < _boxesPerAxis; y++)
                {
                    int i = GetId(new Int2() { X = x, Y = y });
                    _boxesA[i] = new Box(_random, new Vector3() { X = x * boxSize, Y = y * boxSize }, new Vector3() { X = (x +1) * boxSize, Y = (y + 1) * boxSize }, 16, _nullParticle);
                    _boxesB[i] = new Box(_random, new Vector3() { X = x * boxSize, Y = y * boxSize }, new Vector3() { X = (x + 1) * boxSize, Y = (y + 1) * boxSize }, 16, _nullParticle);
                }
            }


            /*for(int i = 1; i < _boxesA.Length; i++)
            {
                for(int j = 0; j < _boxesA[0].Particles.Length; j++)
                {
                    _boxesA[i].Particles[j] = new Particle(_nullParticle);
                    _boxesB[i].Particles[j] = new Particle(_nullParticle);
                }
            }
            for (int j = 1; j < _boxesA[0].Particles.Length; j++)
            {
                _boxesA[0].Particles[j] = new Particle(_nullParticle);
                _boxesB[0].Particles[j] = new Particle(_nullParticle);
            }*/

            _currentBoxes = _boxesA;
            _previousBoxes = _boxesB;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RunSimulation()
        {
            ClearBackbuffer();
            DrawGrid();

            CalcDensities();
            UpdateParticles();
            UpdateBoundary();
            DrawParticles();
            FlipBuffers();

            if (_framecount % 10 == 0)
            {
                ReassignParticles();
                FlipBuffers();
            }

            _framecount++;

            BackBufferSource =
                BitmapSource.Create(
                    _renderWidth,
                    _renderHeight,
                    96, 96,
                    PixelFormats.Bgra32,
                    null,
                    _backBuffer,
                    _backbufferStride);
            
            int totalParticles = _boxesA.Sum(box => box.Particles.Count(p => p.Id != _nullParticle.Id));


            OnPropertyChanged(nameof(BackBufferSource));
        }

        private void CalcDensities()
        {
            for (int i = 0; i < _currentBoxes.Length; i++)
            {
                var coord = GetLocFromId(i);
                var writeBox = _currentBoxes[i];

                foreach (var writeParticle in writeBox.Particles)
                {
                    writeParticle.Density = 0.0f;
                    foreach (var dir in _directions)
                    {
                        var readBox = GetBox(coord + dir, _previousBoxes);

                        foreach(var readParticle in readBox.Particles)
                        {
                            if (readParticle.Id == writeParticle.Id) continue;

                            var delta = readParticle.Position - writeParticle.Position;
                            var dist = delta.Length;

                            var densityContribution = MathsStuff.Lerp(0.0f, readParticle.Mass, MathF.Max(dist / (_renderWidth / _boxesPerAxis), 1.0f));
                            writeParticle.Density += densityContribution;
                        }
                    }
                }
            }
        }

        private void DrawGrid()
        {
            foreach(var box in _currentBoxes)
            {
                for(int x = (int)box.Lower.X; x < (int)box.Upper.X; x++)
                {
                    var id = ToBackbufferID(x, (int)box.Upper.Y);
                    if (id == -1) continue;
                    _backBuffer[id + 0] = 32;
                    _backBuffer[id + 1] = 32;
                    _backBuffer[id + 2] = 32;
                }

                for (int y = (int)box.Lower.Y; y < (int)box.Upper.Y; y++)
                {
                    var id = ToBackbufferID((int)box.Upper.X, y);
                    if (id == -1) continue;
                    _backBuffer[id + 0] = 32;
                    _backBuffer[id + 1] = 32;
                    _backBuffer[id + 2] = 32;
                }
            }
        }

        private void UpdateBoundary()
        {
            for(int y = 0; y < _boxesPerAxis; y ++)
            {
                var box = GetBox(new Int2() { X = 0, Y = y }, _currentBoxes);

                var nonNullParticles = box.Particles.Count(p => p.Id != _nullParticle.Id);

                for(int i = nonNullParticles; i < 8; i++)
                {
                    int j = 0;
                    while(box.Particles[j].Id != _nullParticle.Id)
                    {
                        j++;
                    }

                    box.Particles[j] = new Particle(_random, _renderWidth / _boxesPerAxis, box.Lower.X, box.Lower.Y);
                }

                for(int i = 0; i < box.Particles.Length; i++)
                {
                    var particle = box.Particles[i];
                    if(particle.Id == _nullParticle.Id)
                    {
                        continue;
                    }
                    box.Particles[i].Mass = 1.0f;
                    box.Particles[i].Velocity.X = 1.0f;
                }
            }
        }

        private void ReassignParticles()
        {
            for(int i = 0; i < _currentBoxes.Length; i++)
            {
                var coord = GetLocFromId(i);
                var particlesWithinBounds = new List<Particle>();
                var writeBox = _currentBoxes[i];

                foreach(var dir in _directions)
                {
                    var readBox = GetBox(coord + dir, _previousBoxes);

                    foreach(var particle in readBox.Particles)
                    {
                        if(particle.Position.X >= writeBox.Lower.X &&
                            particle.Position.Y >=  writeBox.Lower.Y &&
                            particle.Position.X < writeBox.Upper.X &&
                            particle.Position.Y < writeBox.Upper.Y &&
                            particle.Id != _nullParticle.Id)
                        {
                            particlesWithinBounds.Add(particle);
                        }
                    }
                }

                float massLost = 0.0f;
                Vector3 momentumLost = new Vector3();
                if (particlesWithinBounds.Count > 16)
                {
                    while (particlesWithinBounds.Count > 12)
                    {
                        var toRemove = particlesWithinBounds[_random.Next(0, particlesWithinBounds.Count - 1)];
                        massLost += toRemove.Mass;
                        momentumLost += toRemove.Velocity * toRemove.Mass;
                        particlesWithinBounds.Remove(toRemove);
                    }
                    var massToAddPerParticle = massLost / 12.0f;
                    var velocityToAddPerParticle = (momentumLost / massLost) / 12.0f;
                    foreach(var particle in particlesWithinBounds)
                    {
                        particle.Mass += massToAddPerParticle;
                        particle.Velocity += velocityToAddPerParticle;
                    }
                }
                /*else if (particlesWithinBounds.Count < 4)
                {
                    while (particlesWithinBounds.Count < 4 && particlesWithinBounds.Count > 0)
                    {
                        var toReplicate = particlesWithinBounds[_random.Next(0, particlesWithinBounds.Count - 1)];
                        if (toReplicate.Id == _nullParticle.Id)
                        {
                            particlesWithinBounds.Add(new Particle(_random, _renderWidth / _boxesPerAxis, writeBox.Lower.X, writeBox.Lower.Y));
                        }
                        else
                        {
                            particlesWithinBounds.Add(new Particle(_random, toReplicate));
                        }
                    }
                }*/

                for (int j = 0; j < writeBox.Particles.Length; j++)
                {
                    if(j < particlesWithinBounds.Count)
                    {
                        writeBox.Particles[j] = new Particle(particlesWithinBounds[j]);
                    }
                    else
                    {
                        writeBox.Particles[j] = new Particle(_nullParticle);
                    }
                }
            }

            for(int i = 0; i < _currentBoxes.Length; i++)
            {
                var currentBox = _currentBoxes[i];
                var previousBox = _previousBoxes[i];

                for(int j = 0; j < currentBox.Particles.Length; j++)
                {
                    previousBox.Particles[j] = new Particle(currentBox.Particles[j]);
                }
            }
        }

        private Box GetBox(Int2 loc, Box[] boxes)
        {
            var id = GetId(loc);
            return id != -1 ? boxes[id] : _nullBox;
        }

        private int GetId(Int2 loc)
        {
            if (loc.X < 0) return -1;
            if (loc.X >= _boxesPerAxis) return -1;
            if (loc.Y < 0) return -1;
            if (loc.Y >= _boxesPerAxis) return -1;

            return loc.X + loc.Y * _boxesPerAxis;
        }

        private Int2 GetLocFromId(int i)
        {
            return new Int2() { X = i % _boxesPerAxis, Y = i / _boxesPerAxis };
        }

        private void UpdateParticles()
        {
            for (int i = 0; i < _currentBoxes.Length; i++)
            {
                var currentBox = _currentBoxes[i];
                var previousBox = _previousBoxes[i];

                for (int j = 0; j < currentBox.Particles.Length; j++)
                {
                    var particle = currentBox.Particles[j];
                    var oldParticle = previousBox.Particles[j];

                    if(oldParticle == _nullParticle)
                    {
                        currentBox.Particles[j] = _nullParticle;
                        continue;
                    }

                    if(particle == _nullParticle)
                    {
                        int a = 9;
                    }
                    if(oldParticle.Mass != particle.Mass)
                    {
                        int a = 0;
                    }

                    particle.Position = oldParticle.Position + oldParticle.Velocity;
                    particle.Velocity = oldParticle.Velocity;

                    /*if (particle.Position.X >= 1020 && particle.Velocity.X > 0)
                    {
                        particle.Velocity.X = -particle.Velocity.X;
                    }
                    if (particle.Position.Y >= 1020 && particle.Velocity.Y > 0)
                    {
                        particle.Velocity.Y = -particle.Velocity.Y;
                    }
                    if (particle.Position.X < 5 && particle.Velocity.X < 0)
                    {
                        particle.Velocity.X = -particle.Velocity.X;
                    }
                    if (particle.Position.Y < 5 && particle.Velocity.Y < 0)
                    {
                        particle.Velocity.Y = -particle.Velocity.Y;
                    }*/
                }
            }
        }

        private void FlipBuffers()
        {
            var temp = _currentBoxes;
            _currentBoxes = _previousBoxes;
            _previousBoxes = temp;
        }

        private void DrawParticles()
        {
            foreach (var box in _currentBoxes)
            {
                foreach (var particle in box.Particles)
                {
                    int backBufferId = ToBackbufferID((int)particle.Position.X, (int)particle.Position.Y);
                    if (backBufferId != -1)
                    {
                        //int col = (int)(particle.Mass * 128.0f);
                        int col = (int)(particle.Density * 1.0f);
                        if (col > 255) col = 255;
                        if (col < 0) throw new Exception();
                        _backBuffer[backBufferId + 0] = (byte)col;
                        _backBuffer[backBufferId + 1] = (byte)col;
                        _backBuffer[backBufferId + 2] = (byte)col;
                    }
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
