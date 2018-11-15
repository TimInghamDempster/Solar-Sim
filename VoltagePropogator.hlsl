RWTexture2D<float4> Voltages;
Texture2D OldVoltages;

[numthreads(32, 32, 1)]
void main( uint3 threadID : SV_DispatchThreadID )
{
	Voltages[threadID.xy] = float4(threadID.xy / 1024.0f, 0, 1);
}
