using SlimDX;
using SlimDX.Direct3D11;
using System;

namespace SlimDXHelpers
{
    public class TextureAndViews : IDisposable
    {
        private readonly Resource _data;
        private readonly Device _device;
        private readonly SlimDX.DXGI.Format _format;
        public ItemCount<Pixel> Width { get; }
        public ItemCount<Pixel> Height { get; }

        public ShaderResourceView SRV { get; }
        public UnorderedAccessView UAV { get; }
        public MemorySize RowPitch => _format.FormatElementSizeBits() * Width;
        public MemorySize ResourceSize => RowPitch * Height;

        public TextureAndViews(Device device, SlimDX.DXGI.Format format, ItemCount<Pixel> width, ItemCount<Pixel> height, DataRectangle data = null)
        {
            _device = device;
            _format = format;
            Width = width;
            Height = height;

            var texDesc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height.Count,
                Width = width.Count,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SlimDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Default
            };

            if (data != null)
            {
                _data = new Texture2D(device, texDesc, data);
            }
            else
            {
                _data = new Texture2D(device, texDesc);
            }

            SRV = new ShaderResourceView(device, _data);
            UAV = new UnorderedAccessView(device, _data);
        }
        public void FillWithData(DataStream dataStream)
        {
            var pitch = _format.FormatElementSizeBits() * Width;
            var slicePitch = pitch * Height;

            // This isn't at all robust, but will do for now
            const int bytesPerFloat = 4;

            dataStream.Seek(0, System.IO.SeekOrigin.Begin);

            var dataBox = new DataBox(pitch.RequiredBytes, slicePitch.RequiredBytes, dataStream);
            _device.ImmediateContext.UpdateSubresource(dataBox, _data, 0);
        }

        public void FillRandomFloats(Random random)
        {
            var pitch = _format.FormatElementSizeBits() * Width;
            var slicePitch = pitch * Height;

            // This isn't at all robust, but will do for now
            const int bytesPerFloat = 4;
            var elmCount = _format.FormatElementSizeBits().RequiredBytes / bytesPerFloat;

            var stream = new DataStream(slicePitch.RequiredBytes, true, true);
            for(int x = 0; x < Width.Count; x++)
            {
                for(int y = 0; y < Height.Count; y++)
                {
                    for(int elmId = 0; elmId < elmCount; elmId++)
                    {
                        var offset = elmId == 0 ? 0.0f : 0.5f;
                        stream.Write((float)random.NextDouble() - offset);
                    }
                }
            }

            stream.Seek(0, System.IO.SeekOrigin.Begin);

            var dataBox = new DataBox(pitch.RequiredBytes, slicePitch.RequiredBytes, stream);
            _device.ImmediateContext.UpdateSubresource(dataBox , _data, 0);
        }

        public void Dispose()
        {
            _data.Dispose();
            SRV.Dispose();
            UAV.Dispose();
        }
    }
}
