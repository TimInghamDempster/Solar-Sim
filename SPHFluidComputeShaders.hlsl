RWTexture2D<float4> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
	float mass;
};

StructuredBuffer<Particle> particlesIn : register(t1);
RWStructuredBuffer<Particle> particlesOut : register(t0);

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
};

static const float PI = 3.14159265f;
static const int particlesPerBox = 16;
static const int boxesPerAxis = 64;

int CalcParticleIndex(uint3 boxId, int particleId)
{
	

	int id = particleId;
	id += boxId.x * particlesPerBox;
	id += boxId.y * boxesPerAxis * particlesPerBox;
	id += boxId.z * boxesPerAxis * boxesPerAxis * particlesPerBox;
	return id;
}

float GetSmoothedWeight(float dist, float h)
{
	float q = dist / (2.0f * h);
	float sigma = 1.0f / (PI * pow(h, 3.0f));

	if (q < 1.0f)
	{
		float linearTerm = 1.0f - (q / 2.0f);
		float squareTerm = (3.0f / 2.0f) * pow(q, 2.0f);
		return sigma * (1.0f - (squareTerm * linearTerm));
	}
	else if (q < 2.0f)
	{
		return (sigma / 4.0f) * pow((2.0f - q), 3);
	}
	else
	{
		return 0.0f;
	}
}

[numthreads(4, 4, 4)]
void UpdateParticlePositions(uint3 boxId : SV_DispatchThreadID)
{
	int particlesAssigned = 0;
	for (int idInBox = 0; idInBox < 16; idInBox++)
	{
		int thisParticleId = CalcParticleIndex(boxId, idInBox);

		for (int dz = -1; dz < 2; dz++)
		{
			for (int dy = -1; dy < 2; dy++)
			{
				for (int dx = -1; dx < 2; dx++)
				{
					int3 localBoxId = boxId;
					localBoxId.x += dx;
					localBoxId.y += dy;
					localBoxId.z += dz;

					int particleId = CalcParticleIndex(localBoxId, idInBox);

					float3 startPos = particlesIn[particleId].position;
					float3 velocity = particlesIn[particleId].velocity;
					float3 updatedPos = startPos + (velocity * 0.5f);

					// Need to go explicitly via a float to get the rounding right
					float3 globalPos = localBoxId + updatedPos;
					int3 updatedBoxId = globalPos;
					
					if (updatedBoxId.x == boxId.x && updatedBoxId.y == boxId.y && updatedBoxId.z == boxId.z)
					{
						if (particlesAssigned < 16)
						{
							int outId = CalcParticleIndex(boxId, particlesAssigned);
							particlesOut[outId].position = frac(updatedPos);
							particlesOut[outId].velocity = velocity;
							particlesOut[outId].mass = particlesIn[particleId].mass;
							particlesAssigned++;
						}
						else
						{
							float bestDist = 10000.0f; // Bignum
							int bestId = 0;
							for (int comparisonParticleId = 0; comparisonParticleId < 16; comparisonParticleId++)
							{
								int testId = CalcParticleIndex(boxId, comparisonParticleId);
								float3 vel = particlesOut[testId].velocity;
								float distSq = dot(vel, vel);
								if (distSq < bestDist)
								{
									bestDist = distSq;
									bestId = comparisonParticleId;
								}
							}
							int bestParticleId = CalcParticleIndex(boxId, bestId);
							particlesOut[bestParticleId].mass += particlesIn[particleId].mass;
						}
					}
				}
			}
		}

		for (int index = particlesAssigned; index < 16; index++)
		{
			int outId = CalcParticleIndex(boxId, particlesAssigned);
			particlesOut[outId].position = 0.0f;
			particlesOut[outId].velocity = 0.0f;
			particlesOut[outId].mass = 0.0f;
		}
	}
}

[numthreads(16, 16, 1)]
void OutputPressures(uint3 groupId : SV_GroupID, uint3 threadId : SV_GroupThreadID)
{
	int renderHeight = 720;
	int2 pixelLoc = groupId.xy * 16 + threadId.xy;
	
	int3 boxId = 32;
	boxId.xy = pixelLoc * 64 / renderHeight;
	int2 cornerPos = boxId.xy * renderHeight / 64;
	float3 pixelPos = 32.5f;
	pixelPos.xy = pixelLoc * 64.0f / 720.0f * 1.0f;

	float brightness = 0.0f;

	for (int dy = -1; dy < 2; dy++)
	{
		for (int dx = -1; dx < 2; dx++)
		{
			for (int idInBox = 0; idInBox < 16; idInBox++)
			{
				int3 localBoxId = boxId;
				localBoxId.x += dx;
				localBoxId.y += dy;
				float3 boxPos = localBoxId;

				int firstParticleId = CalcParticleIndex(localBoxId, 0);
				float3 particlePos = particlesIn[firstParticleId + idInBox].position + float3(0.0f, 0.0f, 0.0f) + boxPos;
				float particleMass = particlesIn[firstParticleId + idInBox].mass;
				float3 delta = pixelPos - particlePos;

				float dist = length(delta);

				brightness += GetSmoothedWeight(dist, 0.4f) / (16.0f * 9.0f) * particleMass;
				//brightness += particleMass / dist / 1000.0f;
			}
		}
	}

	float4 col = 1.0f;
	col.xyz = brightness;
	outputImage[pixelLoc] = col;
}

[numthreads(16, 1, 1)]
void OutputParticles(uint3 boxId : SV_GroupID, uint3 threadId : SV_GroupThreadID)
{
	float scaleFactor = 100.0f;

	boxId.z = 32;

	int particleId = CalcParticleIndex(boxId, threadId.x);

	float3 pos = particlesIn[particleId].position;
	int2 outputPos = boxId.xy * scaleFactor + pos.xy * scaleFactor;
	float mass = particlesIn[particleId].mass;

	if (mass > 0.0f)
	{
		outputImage[outputPos] = 0.5f;
	}
}
