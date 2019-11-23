RWTexture2D<float4> outputImage : register(t#OutputSlot#);

Texture2D<float4> ReadGrid : register(t#GridReadSlot#);
RWTexture2D<float4> WriteGrid : register(t#GridWriteSlot#);

SamplerState GridSampler : register (s0)
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

[numthreads(#OutputThreads#, #OutputThreads#, 1)]
void OutputGrid(uint3 threadID : SV_DispatchThreadID)
{
	float2 sampleSite = ((float2)threadID.xy + float2(0.5f, 0.5f)) / #Resolution#.0f;
	
	float4 vel = ReadGrid.SampleLevel(GridSampler, sampleSite, 0);

	/*float mass = vel.x;
	float outflow = mass * length(vel.yz);
	mass -= outflow;
	
	vel.x = max(mass, 0.0f);

	WriteGrid[threadID.xy] = vel;*/

	outputImage[threadID.xy] = vel.x >= 0 ? vel .x : float4(0.5f, 0.0f, 0.0f, 1.0f);
}