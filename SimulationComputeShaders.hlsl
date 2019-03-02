RWTexture2D<float> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
};

StructuredBuffer<Particle> particlesIn : register(t1);
RWStructuredBuffer<Particle> particlesOut : register(t0);

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
	float boxHalfSize;
};

[numthreads(8, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_DispatchThreadID)
{
	int3 pos = particlesIn[threadID.x].position * int3(6, 6, 1) + int3(640, 360, 0);
	
	outputImage[pos.xy] = 0.9f;
}

[numthreads(8,1,1)]
void UpdateParticelPositions(uint3 threadID : SV_DispatchThreadID)
{
	float3 myPos = particlesIn[threadID.x].position;
	float3 acceleration = 0.0f;

	float3 velocity = particlesIn[threadID.x].velocity + acceleration * timestepInYears * 0.1;

	float3 newPos = particlesIn[threadID.x].position + (velocity * timestepInYears);
	
	if (newPos.x > boxHalfSize && velocity.x > 0)
	{
		velocity.x *= -1.0f;
	}
	if (newPos.x < -boxHalfSize && velocity.x < 0)
	{
		velocity.x *= -1.0f;
	}
	if (newPos.y > boxHalfSize && velocity.y > 0)
	{
		velocity.y *= -1.0f;
	}
	if (newPos.y < -boxHalfSize && velocity.y < 0)
	{
		velocity.y *= -1.0f;
	}
	if (newPos.z > boxHalfSize && velocity.z > 0)
	{
		velocity.z *= -1.0f;
	}
	if (newPos.z < -boxHalfSize && velocity.z < 0)
	{
		velocity.z *= -1.0f;
	}

	particlesOut[threadID.x].position = newPos;
	particlesOut[threadID.x].velocity = velocity;
}