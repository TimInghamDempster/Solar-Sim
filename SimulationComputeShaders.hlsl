RWTexture2D<float> outputImage;

struct ParticleBox
{
	int3 positions[8];
};

StructuredBuffer<ParticleBox> particlesOut : register(t1);

[numthreads(8, 1, 1)]
void OutputParticlePoints( uint3 threadID : SV_GroupThreadID, uint3 threadGroupID : SV_GroupID )
{
	int3 pos = particlesOut[threadGroupID.x].positions[threadID.x];
	
	outputImage[pos.xy] = 0.5f;
}