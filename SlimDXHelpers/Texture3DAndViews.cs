using SlimDX;
using SlimDX.Direct3D11;
using System;

namespace SlimDXHelpers
{
    public class Texture3DAndViews : IDisposable
    {
        private readonly Resource _data;

        public ItemCount<Pixel> Width { get; }
        public ItemCount<Pixel> Height { get; }
        public ItemCount<Pixel> Depth { get; }

        public ShaderResourceView SRV { get; }
        public UnorderedAccessView UAV { get; }

        public Texture3DAndViews(Device device, SlimDX.DXGI.Format format, ItemCount<Pixel> width, ItemCount<Pixel> height, ItemCount<Pixel> depth, DataBox[] data = null)
        {
            Width = width;
            Height = height;
            Depth = depth;

            var texDesc = new Texture3DDescription()
            {
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Depth = depth.Count,
                Format = format,
                Height = height.Count,
                Width = width.Count,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };

            if (data != null)
            {
                _data = new Texture3D(device, texDesc, data);
            }
            else
            {
                _data = new Texture3D(device, texDesc);
            }

            SRV = new ShaderResourceView(device, _data);
            UAV = new UnorderedAccessView(device, _data);
        }
       
        public void Dispose()
        {
            _data.Dispose();
            SRV.Dispose();
            UAV.Dispose();
        }
    }
}
