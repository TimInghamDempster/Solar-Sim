RWTexture2D<float4> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
};

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
};

[numthreads(8,8,1)]
void OutputPressures(uint3 threadId : SV_DispatchThreadID)
{
	outputImage[threadId.xy] = 0.5f;
}
