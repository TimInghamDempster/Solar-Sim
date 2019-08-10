using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System;
using System.Drawing;

namespace SlimDXHelpers
{
    /// <summary>
    /// The final stage shader which draws the image we have computed
    /// to the screen (or any other SRV)
    /// </summary>
    public class FSQ
    {
        private readonly SlimDX.Direct3D11.Buffer _quadVertices;
        private readonly InputLayout _layout;
        private readonly Texture2D _renderTexture;
        private readonly ShaderResourceView _srv;
        private readonly Effect _effect;
        private readonly EffectTechnique _technique;
        private readonly EffectPass _pass;
        private readonly SlimDX.Direct3D11.Device _device;
        private readonly RenderTargetView _renderView;

        public UnorderedAccessView UAV { get; }

        public FSQ(SlimDX.Direct3D11.Device device, RenderTargetView renderView, string effectFileName)
        {
            _device = device ??
                throw new ArgumentNullException(nameof(device));

            _renderView = renderView ??
                throw new ArgumentNullException(nameof(renderView));

            const int vertexSizeInBytes = 32;

            var bytecode = ShaderBytecode.CompileFromFile(effectFileName, "fx_5_0", ShaderFlags.None, EffectFlags.None);
            _effect = new Effect(device, bytecode);

            _technique = _effect.GetTechniqueByIndex(0);
            _pass = _technique.GetPassByIndex(0);

            _layout = new InputLayout(
                device,
                _pass.Description.Signature,
                new[] {
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 16, 0)});

            var stream = new DataStream(vertexSizeInBytes * 6, true, true);

            stream.Write(new Vector4(-1.0f, -1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(0.0f, 1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(-1.0f, 1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(0.0f, 0.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(1.0f, -1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(1.0f, 1.0f, 0.5f, 1.0f));

            stream.Write(new Vector4(1.0f, 1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(1.0f, 0.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(-1.0f, 1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(0.0f, 0.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(1.0f, -1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(1.0f, 1.0f, 0.5f, 1.0f));
            stream.Position = 0;

            _quadVertices = new SlimDX.Direct3D11.Buffer(device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 6 * vertexSizeInBytes,
                Usage = ResourceUsage.Default
            });

            stream.Dispose();

            Texture2DDescription renderTexDesc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                Height = 1080,
                Width = 1080,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription()
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = ResourceUsage.Default
            };
            _renderTexture = new Texture2D(device, renderTexDesc);
            _srv = new ShaderResourceView(device, _renderTexture);
            UAV = new UnorderedAccessView(device, _renderTexture);

            bytecode.Dispose();
        }

        public void Draw()
        {
            _device.ImmediateContext.ClearRenderTargetView(_renderView, Color.Blue);

            _device.ImmediateContext.InputAssembler.InputLayout = _layout;
            _device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadVertices, 32, 0));

            _effect.GetVariableByName("tx").AsResource().SetResource(_srv);

            for (int i = 0; i < _technique.Description.PassCount; ++i)
            {
                _pass.Apply(_device.ImmediateContext);
                _device.ImmediateContext.Draw(6, 0);
            }

            _effect.GetVariableByName("tx").AsResource().SetResource(null);
        }

        public void Dispose()
        {
            _quadVertices.Dispose();
            _layout.Dispose();
            _renderTexture.Dispose();
            _srv.Dispose();
            UAV.Dispose();
            _effect.Dispose();
    }
    }
}
