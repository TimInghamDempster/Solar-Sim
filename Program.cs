﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using Device = SlimDX.Direct3D11.Device;

namespace Micro_Architecture
{
    class Program
    {
        static Device device;
        static SwapChain swapChain;
        static InputLayout Layout;
        static RenderTargetView renderView;
        static SlimDX.Direct3D11.Buffer quadVertices;
        static Effect effect;
        static ComputeShader particlePointOutputCS;
        static ComputeShader voltageInputCS;
        static EffectTechnique technique;
        static EffectPass pass;
        static Vector2 scale;
        static Vector2 pos;
        static Texture2D renderTexture;
        static ShaderResourceView renderSRV;
        static UnorderedAccessView renderUAV;

        static bool leftDown = false;

        static void Main(string[] args)
        {
            var form = InitD3D();
            InitRAWInput();
            InitRenderTextureAndVB();

            MessagePump.Run(form, SimMain);
            
            quadVertices.Dispose();
            renderView.Dispose();
            device.Dispose();
            swapChain.Dispose();

        }
        static SlimDX.Windows.RenderForm InitD3D()
        {
            var form = new RenderForm("Micro Architecture");

            form.Width = 1920;
            form.Height = 1080;

            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out device, out swapChain);

            Factory factory = swapChain.GetParent<Factory>();
            factory.SetWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(device, backBuffer);
            var bytecode = ShaderBytecode.CompileFromFile("MiniTri.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None);
            effect = new Effect(device, bytecode);
            var csBytecode = ShaderBytecode.CompileFromFile("SimulationComputeShaders.hlsl", "OutputParticlePoints", "cs_5_0", ShaderFlags.None, EffectFlags.None);
            particlePointOutputCS = new ComputeShader(device, csBytecode);
            var inputBytecode = ShaderBytecode.CompileFromFile("VoltagePropogator.hlsl", "UpdateInputPins", "cs_5_0", ShaderFlags.None, EffectFlags.None);
            voltageInputCS = new ComputeShader(device, inputBytecode);
            technique = effect.GetTechniqueByIndex(0);
            pass = technique.GetPassByIndex(0);

            RasterizerStateDescription rsd = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                FillMode = FillMode.Solid,
                IsAntialiasedLineEnabled = false,
                IsDepthClipEnabled = false,
                IsFrontCounterclockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = 0.0f
            };

            RasterizerState rs = RasterizerState.FromDescription(device, rsd);
            device.ImmediateContext.Rasterizer.State = rs;

            device.ImmediateContext.OutputMerger.SetTargets(renderView);
            device.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));

            scale = new Vector2(1000.0f / 1920.0f, 1000.0f / 1080.0f);
            pos = new Vector2(0.0f, 0.0f);

            /*bytecode.Dispose();
            effect.Dispose();
            backBuffer.Dispose();*/

            return form;
        }

        static void InitRAWInput()
        {
            SlimDX.RawInput.Device.RegisterDevice(SlimDX.Multimedia.UsagePage.Generic, SlimDX.Multimedia.UsageId.Mouse, SlimDX.RawInput.DeviceFlags.None);
            SlimDX.RawInput.Device.MouseInput += new System.EventHandler<SlimDX.RawInput.MouseInputEventArgs>(Device_MouseInput);
        }

       

        static void Device_MouseInput(object sender, SlimDX.RawInput.MouseInputEventArgs e)
        {
            if ((e.ButtonFlags & SlimDX.RawInput.MouseButtonFlags.LeftDown) == SlimDX.RawInput.MouseButtonFlags.LeftDown)
                leftDown = true;
            if ((e.ButtonFlags & SlimDX.RawInput.MouseButtonFlags.LeftUp) == SlimDX.RawInput.MouseButtonFlags.LeftUp)
                leftDown = false;

            if (leftDown)
            {
                pos.X += e.X  * 3.0f / 1920.0f;
                pos.Y -= e.Y * 3.0f / 1080.0f;
            }

            scale.X -= e.WheelDelta / 1920.0f;
            scale.Y -= e.WheelDelta / 1080.0f;

            if (scale.X < 0.0f)
            {
                scale.X = 100.0f / 1920.0f;
                scale.Y = 100.0f / 1080.0f;
            }
        }

        /// <summary>
        /// The main loop function that gets called by the SlimDX message pump,
        /// specifically do a simulation update step then draw the result to the
        /// screen
        /// </summary>
        static void SimMain()
        {
            OutputSimulationResults();
            DrawRenderTextureToScreen(); 

            swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Draws a full screen quad with the texture generated by the simulation
        /// output stage
        /// </summary>
        static void DrawRenderTextureToScreen()
        {
            device.ImmediateContext.ClearRenderTargetView(renderView, Color.Black);

            device.ImmediateContext.InputAssembler.InputLayout = Layout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(quadVertices, 32, 0));

            effect.GetVariableByName("tx").AsResource().SetResource(renderSRV);

            for (int i = 0; i < technique.Description.PassCount; ++i)
            {
                pass.Apply(device.ImmediateContext);
                device.ImmediateContext.Draw(6, 0);
            }

            effect.GetVariableByName("tx").AsResource().SetResource(null);
        }

        static void OutputSimulationResults()
        {
            device.ImmediateContext.ComputeShader.Set(particlePointOutputCS);
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(renderUAV, 0);
            device.ImmediateContext.Dispatch(1280 / 32, 720 / 32, 1);
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }

        /// <summary>
        /// Create the render texture that will display the state of the simulation,
        /// also the VB for drawing a full screen quad, and the resource views
        /// </summary>
        static void InitRenderTextureAndVB()
        {

            const int vertexSizeInBytes = 32;

            Layout = new InputLayout(device, pass.Description.Signature, new[] {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 16, 0)
            });
            
            var stream = new DataStream(vertexSizeInBytes * 6, true, true);
            
            stream.Write(new Vector4(-1.0f, -1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 0.0f,  1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(-1.0f,  1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 0.0f,  0.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 1.0f, -1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 1.0f,  1.0f, 0.5f, 1.0f));

            stream.Write(new Vector4( 1.0f,  1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 1.0f,  0.0f, 0.5f, 1.0f));
            stream.Write(new Vector4(-1.0f,  1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 0.0f,  0.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 1.0f, -1.0f, 0.5f, 1.0f));
            stream.Write(new Vector4( 1.0f,  1.0f, 0.5f, 1.0f));
            stream.Position = 0;

            quadVertices = new SlimDX.Direct3D11.Buffer(device, stream, new BufferDescription()
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
                Height = 720,
                Width = 1280,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription()
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = ResourceUsage.Default
            };
            renderTexture = new Texture2D(device, renderTexDesc);
            renderSRV = new ShaderResourceView(device, renderTexture);
            renderUAV = new UnorderedAccessView(device, renderTexture);
        }
    }
}
