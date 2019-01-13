RWTexture2D<float> outputImage : register(t0);

struct ParticleBox
{
	int3 positions[8];
};

StructuredBuffer<ParticleBox> particlesIn : register(t1);
RWStructuredBuffer<ParticleBox> particlesOut : register(t0);

[numthreads(8, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_GroupThreadID, uint3 threadGroupID : SV_GroupID )
{
	int3 pos = particlesIn[threadGroupID.x].positions[threadID.x];
	
	outputImage[pos.xy] = 0.5f;
}

[numthreads(8,1,1)]
void UpdateParticelPositions(uint3 threadID : SV_GroupThreadID, uint3 threadGroupID : SV_GroupID)
{
	int3 pos = particlesIn[threadGroupID.x].positions[threadID.x];

	particlesOut[threadGroupID.x].positions[threadID.x].x = pos.x + 1;
	particlesOut[threadGroupID.x].positions[threadID.x].y = pos.y;
}