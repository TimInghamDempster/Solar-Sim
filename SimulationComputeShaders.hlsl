RWTexture2D<float> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
};

StructuredBuffer<Particle> particlesIn : register(t2);
RWStructuredBuffer<Particle> particlesOut : register(t0);
RWTexture3D<uint> pressureGrid : register(t1);
Texture3D<uint> pressureView : register(t1);

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
	float simulationHalfSize;
	float simulationSize;
	float gridAxisSize;
};

[numthreads(8, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_DispatchThreadID)
{
	int3 pos = particlesIn[threadID.x].position * int3(6, 6, 1) + int3(640, 360, 0);
	
	outputImage[pos.xy] = 0.9f;
}

[numthreads(8,8,1)]
void OutputPressures(uint3 threadId : SV_DispatchThreadID)
{
	uint3 boxIndex = threadId;
	boxIndex.z = gridAxisSize / 2.0f;

	if (pressureView[boxIndex] != 0)
	{
		outputImage[boxIndex.xy] = (float)pressureView[boxIndex] / 100.0f;
	}
}

[numthreads(8,1,1)]
void UpdateParticelPositions(uint3 threadID : SV_DispatchThreadID)
{
	float3 myPos = particlesIn[threadID.x].position;
	float3 acceleration = 0.0f;

	float3 velocity = particlesIn[threadID.x].velocity + acceleration * timestepInYears * 0.1;

	float3 newPos = particlesIn[threadID.x].position + (velocity * timestepInYears);
	
	if (newPos.x > simulationHalfSize && velocity.x > 0)
	{
		velocity.x *= -1.0f;
	}
	if (newPos.x < -simulationHalfSize && velocity.x < 0)
	{
		velocity.x *= -1.0f;
	}
	if (newPos.y > simulationHalfSize && velocity.y > 0)
	{
		velocity.y *= -1.0f;
	}
	if (newPos.y < -simulationHalfSize && velocity.y < 0)
	{
		velocity.y *= -1.0f;
	}
	if (newPos.z > simulationHalfSize && velocity.z > 0)
	{
		velocity.z *= -1.0f;
	}
	if (newPos.z < -simulationHalfSize && velocity.z < 0)
	{
		velocity.z *= -1.0f;
	}

	int3 index = (newPos + simulationHalfSize) / simulationSize * gridAxisSize;
	InterlockedAdd(pressureGrid[index], 1);

	particlesOut[threadID.x].position = newPos;
	particlesOut[threadID.x].velocity = velocity;
}