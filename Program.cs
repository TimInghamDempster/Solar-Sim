using System;
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
        static InputLayout wireLayout;
        static InputLayout gateLayout;
        static RenderTargetView renderView;
        static SlimDX.Direct3D11.Buffer wireVertices;
        static Int32 wireCount;
        static Int32 gateCount;
        static SlimDX.Direct3D11.Buffer gateVertices;
        static Effect effect;
        static Effect circuitSimulationCSEffect;
        static EffectTechnique wireTechnique;
        static EffectTechnique gateTechnique;
        static EffectPass wirePass;
        static EffectPass gatePass;
        static EffectVariable viewPos;
        static EffectVariable viewScale;
        static Vector2 scale;
        static Vector2 pos;
        static bool LeftDown;
        static Texture2D gateTexture;
        static ShaderResourceView gateSRV;
        static Texture2D voltages1;
        static Texture2D voltages2;
        static ShaderResourceView voltagesSRV1;
        static ShaderResourceView voltagesSRV2;
        static UnorderedAccessView voltagesUAV1;
        static UnorderedAccessView voltagesUAV2;
        static EffectResourceVariable voltagesShaderTexture;

        static bool isSimulating = false;

        static void Main(string[] args)
        {
            var form = InitD3D();
            InitRAWInput();
            LoadCircuit();
            InitVoltageTextures();

            MessagePump.Run(form, SimMain);

            wireVertices.Dispose();
            gateVertices.Dispose();
            wireLayout.Dispose();
            renderView.Dispose();
            device.Dispose();
            swapChain.Dispose();

        }

        static void InitRAWInput()
        {
            SlimDX.RawInput.Device.RegisterDevice(SlimDX.Multimedia.UsagePage.Generic, SlimDX.Multimedia.UsageId.Mouse, SlimDX.RawInput.DeviceFlags.None);
            SlimDX.RawInput.Device.MouseInput += new System.EventHandler<SlimDX.RawInput.MouseInputEventArgs>(Device_MouseInput);
        }

        static void InitVoltageTextures()
        {
            Texture2DDescription desc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8_UInt,
                Height = 1024,
                Width = 1024,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription()
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = ResourceUsage.Default
            };

            voltages1 = new Texture2D(device, desc);
            voltages2 = new Texture2D(device, desc);

            voltagesSRV1 = new ShaderResourceView(device, voltages1);
            voltagesUAV1 = new UnorderedAccessView(device, voltages1);

            voltagesSRV2 = new ShaderResourceView(device, voltages2);
            voltagesUAV2 = new UnorderedAccessView(device, voltages2);
        }

        static void Device_MouseInput(object sender, SlimDX.RawInput.MouseInputEventArgs e)
        {
            if ((e.ButtonFlags & SlimDX.RawInput.MouseButtonFlags.LeftDown) == SlimDX.RawInput.MouseButtonFlags.LeftDown)
                LeftDown = true;
            if ((e.ButtonFlags & SlimDX.RawInput.MouseButtonFlags.LeftUp) == SlimDX.RawInput.MouseButtonFlags.LeftUp)
                LeftDown = false;

            if (LeftDown)
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
            var csBytecode = ShaderBytecode.CompileFromFile("VoltagePropogator.hlsl", "fx_5_0", ShaderFlags.None, EffectFlags.None);
            circuitSimulationCSEffect = new Effect(device, csBytecode);
            wireTechnique = effect.GetTechniqueByIndex(0);
            wirePass = wireTechnique.GetPassByIndex(0);
            gateTechnique = effect.GetTechniqueByIndex(1);
            gatePass = gateTechnique.GetPassByIndex(0);

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

            viewPos = effect.GetVariableByName("viewPos");
            viewScale = effect.GetVariableByName("viewScale");

            scale = new Vector2(1000.0f / 1920.0f, 1000.0f / 1080.0f);
            pos = new Vector2(0.0f, 0.0f);

            /*bytecode.Dispose();
            effect.Dispose();
            backBuffer.Dispose();*/
            
            return form;
        }

        static void SimMain()
        {
            Draw();

            if (isSimulating)
            {
                PropogateSignal();
            }

            swapChain.Present(0, PresentFlags.None);
        }

        static void Draw()
        {
            device.ImmediateContext.ClearRenderTargetView(renderView, Color.Black);

            viewScale.AsVector().Set(scale);
            viewPos.AsVector().Set(pos);

            DrawWires();
            DrawGates();
        }

        /// <summary>
        /// Draws all of the wires in the circuit to the screen, this binds the current voltage srv to the shader
        /// which is used to calculate the colour of the wires for visual feedback on the current state of the circuit
        /// </summary>
        static void DrawWires()
        {
            voltagesShaderTexture.SetResource(voltagesSRV1);

            device.ImmediateContext.InputAssembler.InputLayout = wireLayout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(wireVertices, 20, 0));

            for (int i = 0; i < wireTechnique.Description.PassCount; ++i)
            {
                wirePass.Apply(device.ImmediateContext);
                device.ImmediateContext.Draw(wireCount * 2, 0);
            }
        }

        static void DrawGates()
        {
            device.ImmediateContext.InputAssembler.InputLayout = gateLayout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(gateVertices, 32, 0));

            for (int i = 0; i < gateTechnique.Description.PassCount; ++i)
            {
                gatePass.Apply(device.ImmediateContext);
                device.ImmediateContext.Draw(gateCount * 6, 0);
            }
        }

        static void PropogateSignal()
        {
            
        }

        static void LoadCircuit()
        {
            var circuit = THDL.MicroArchNetwork.GenerateNetwork();

            InitWireBuffer(circuit);
            InitGateBuffer(circuit);
        }

        static void InitWireBuffer(THDL.Network circuit)
        {
            const int wireVertexSizeInBytes = 20;
            wireCount = circuit.Wires.Count;
            wireLayout = new InputLayout(device, wirePass.Description.Signature, new[] {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32_UInt, 16, 0) 
            });

            var stream = new DataStream(wireCount * wireVertexSizeInBytes * 2, true, true);

            foreach (var wire in circuit.Wires)
            {
                stream.Write(new Vector4(wire.Left, wire.Top * -1.0f, 0.5f, 1.0f));
                stream.Write(wire.Input);
                stream.Write(new Vector4(wire.Right, wire.Bottom * -1.0f, 0.5f, 1.0f));
                stream.Write(wire.Input);
            }
            stream.Position = 0;

            wireVertices = new SlimDX.Direct3D11.Buffer(device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 2 * wireVertexSizeInBytes * wireCount,
                Usage = ResourceUsage.Default
            });

            stream.Dispose();
        }

        static void InitGateBuffer(THDL.Network circuit)
        {

            const int gateVertexSizeInBytes = 32;
            gateCount = circuit.Gates.Count;

            gateLayout = new InputLayout(device, wirePass.Description.Signature, new[] {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 16, 0)
            });
            
            var stream = new DataStream(gateCount * gateVertexSizeInBytes * 6, true, true);

            foreach (var gate in circuit.Gates)
            {
                float texGateCount = 2.0f;
                float gateStep = 1.0f / texGateCount;
                float baseU = (Int32)gate.Type * gateStep;
                float topU = baseU + gateStep;

                stream.Write(new Vector4(gate.X + THDL.Gate.Input1XOffset, gate.Y * -1.0f - THDL.Gate.Input1YOffset, 0.5f, 1.0f));
                stream.Write(new Vector4(baseU, 0, 0.5f, 1.0f));
                stream.Write(new Vector4(gate.X + THDL.Gate.Input1XOffset, gate.Y * -1.0f + 0, 0.5f, 1.0f));
                stream.Write(new Vector4(baseU, 1, 0.5f, 1.0f));
                stream.Write(new Vector4(gate.X + THDL.Gate.Input2XOffset, gate.Y * -1.0f - THDL.Gate.Input1YOffset, 0.5f, 1.0f));
                stream.Write(new Vector4(topU, 0, 0.5f, 1.0f));

                stream.Write(new Vector4(gate.X + THDL.Gate.Input2XOffset, gate.Y * -1.0f - 0, 0.5f, 1.0f));
                stream.Write(new Vector4(topU, 1, 0.5f, 1.0f));
                stream.Write(new Vector4(gate.X + THDL.Gate.Input1XOffset, gate.Y * -1.0f + 0, 0.5f, 1.0f));
                stream.Write(new Vector4(baseU, 1, 0.5f, 1.0f));
                stream.Write(new Vector4(gate.X + THDL.Gate.Input2XOffset, gate.Y * -1.0f - THDL.Gate.Input1YOffset, 0.5f, 1.0f));
                stream.Write(new Vector4(topU, 0, 0.5f, 1.0f));
            }

            stream.Position = 0;

            gateVertices = new SlimDX.Direct3D11.Buffer(device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 6 * gateVertexSizeInBytes * gateCount,
                Usage = ResourceUsage.Default
            });

            stream.Dispose();

            gateTexture = Texture2D.FromFile(device, "Gates.png");
            gateSRV = new ShaderResourceView(device, gateTexture);

            effect.GetVariableByName("txGate").AsResource().SetResource(gateSRV);
            voltagesShaderTexture = effect.GetVariableByName("txVoltages").AsResource();
        }
    }
}
