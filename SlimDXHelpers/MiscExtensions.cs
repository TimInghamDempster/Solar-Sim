using SlimDX.DXGI;
using System;

namespace SlimDXHelpers
{
    public static class MiscExtensions
    {
        public static MemorySize FormatElementSizeBits(this Format format)
        {
            switch (format)
            {
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return MemorySize.FromBits(128);

                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return MemorySize.FromBits(96);

                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                case Format.R32G8X24_Typeless:
                case Format.D32_Float_S8X24_UInt:
                case Format.R32_Float_X8X24_Typeless:
                case Format.X32_Typeless_G8X24_UInt:
                    return MemorySize.FromBits(64);

                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                case Format.R11G11B10_Float:
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRGB:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                case Format.X24_Typeless_G8_UInt:
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8X8_UNorm:
                    return MemorySize.FromBits(32);

                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_UInt:
                case Format.R16_SNorm:
                case Format.R16_SInt:
                case Format.B5G6R5_UNorm:
                case Format.B5G5R5A1_UNorm:
                    return MemorySize.FromBits(16);

                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                case Format.A8_UNorm:
                    return MemorySize.FromBits(8);

                // Compressed format; http://msdn2.microsoft.com/en-us/library/bb694531(VS.85).aspx
                case Format.BC2_Typeless:
                case Format.BC2_UNorm:
                case Format.BC2_UNorm_SRGB:
                case Format.BC3_Typeless:
                case Format.BC3_UNorm:
                case Format.BC3_UNorm_SRGB:
                case Format.BC5_Typeless:
                case Format.BC5_UNorm:
                case Format.BC5_SNorm:
                    return MemorySize.FromBits(128);

                // Compressed format; http://msdn2.microsoft.com/en-us/library/bb694531(VS.85).aspx
                case Format.R1_UNorm:
                case Format.BC1_Typeless:
                case Format.BC1_UNorm:
                case Format.BC1_UNorm_SRGB:
                case Format.BC4_Typeless:
                case Format.BC4_UNorm:
                case Format.BC4_SNorm:
                    return MemorySize.FromBits(64);

                // Compressed format; http://msdn2.microsoft.com/en-us/library/bb694531(VS.85).aspx
                case Format.R9G9B9E5_SharedExp:
                    return MemorySize.FromBits(32);

                // These are compressed, but bit-size information is unclear.
                case Format.R8G8_B8G8_UNorm:
                case Format.G8R8_G8B8_UNorm:
                    return MemorySize.FromBits(32);

                case Format.Unknown:
                default:
                    throw new InvalidOperationException("Cannot determine format element size; invalid format specified.");
            }
        }
    }
}
