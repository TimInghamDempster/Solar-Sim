RWTexture2D<float> outputImage : register(t0);

struct ParticleBox
{
	float3 positions[8];
	float3 velocities[8];
};

StructuredBuffer<ParticleBox> particlesIn : register(t1);
RWStructuredBuffer<ParticleBox> particlesOut : register(t0);

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
};

[numthreads(8, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_GroupThreadID, uint3 threadGroupID : SV_GroupID )
{
	int3 pos = particlesIn[threadGroupID.x].positions[threadID.x] * int3(6, 3, 1) + int3(640, 360, 0);
	
	outputImage[pos.xy] = 0.5f;
}

[numthreads(8,1,1)]
void UpdateParticelPositions(uint3 threadID : SV_GroupThreadID, uint3 threadGroupID : SV_GroupID)
{
	float3 pos = particlesIn[threadGroupID.x].positions[threadID.x];
	float3 velocity = particlesIn[threadGroupID.x].velocities[threadID.x];

	particlesOut[threadGroupID.x].positions[threadID.x] = pos + (velocity * timestepInYears);
}