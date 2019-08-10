RWTexture2D<float4> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
	float3 colour;
	float density;
};

struct ParticleBox
{
	int count;
	float3 positions[8];
	float densities[8];
	float3 velocities[8];
};

SamplerState pressureSampler : register (s0)
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

StructuredBuffer<Particle> particlesIn : register(t2);
RWStructuredBuffer<Particle> particlesOut : register(t0);

StructuredBuffer<ParticleBox> particleBoxesIn : register(t3);
RWStructuredBuffer<ParticleBox> particlesBoxesOut : register(t1);

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
	float3 simulationHalfSize;
	float3 simulationSize;
	float gridAxisSize;
};

static const float PI = 3.14159265f;
static const float viscosity = 0.01f;
static const int particlesPerBox = 8;
static const int3 boxesPerAxis = int3(64, 64, 1);

int CalcBoxIndex(uint3 boxId)
{
	int3 sanitizedInId = boxId;
	sanitizedInId.x = min(max(boxId.x, 0), boxesPerAxis.x);
	sanitizedInId.y = min(max(boxId.y, 0), boxesPerAxis.y);
	sanitizedInId.z = min(max(boxId.z, 0), boxesPerAxis.z);

	int id = sanitizedInId.x;
	id += sanitizedInId.y * boxesPerAxis.x;
	id += sanitizedInId.z * boxesPerAxis.y * boxesPerAxis.z;

	return id;
}

float3 CalcRenderPos(float3 pos)
{
	float3 rPos = pos * float3(5.0f, -5.0f, 1.0f) + float3(360.0f, 360.0f, 0.0f);
	return rPos;
}

[numthreads(8, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_DispatchThreadID)
{
	float3 pos = particlesIn[threadID.x].position;
	float3 col = particlesIn[threadID.x].colour;

	// Assumes cube (not cuboid) boxes
	float boxSize = simulationSize.x / boxesPerAxis.x;

	//if (pos.z < boxSize / 2.0f && pos.z > -boxSize / 2.0f)
	{
		float3 renderPos = CalcRenderPos(pos);
		outputImage[renderPos.xy] = float4(col.x, col.y, col.z, 1.0f);
	}
}

[numthreads(8, 1, 1)]
void OutputPressurePoints(uint3 groupId : SV_GroupID, uint3 threadId : SV_GroupThreadID)
{
	int3 boxPos = groupId;
	boxPos.z += 31;
	int boxId = CalcBoxIndex(boxPos);
	int countInBox = particleBoxesIn[boxId].count;

	float3 pos = particleBoxesIn[boxId].positions[threadId.x];

	if (threadId.x < countInBox)
	{
		pos.y *= -1.0f;
		float3 renderPos = CalcRenderPos(pos);

		outputImage[renderPos.xy + int2(1, 1)] = float4(0.0f, 1.0f, 0.0f, 1);
	}
}

int3 WorldPosToBoxPos(float3 worldPos)
{
	return ((worldPos + simulationHalfSize) / simulationSize) * boxesPerAxis;
}

[numthreads(8, 1, 1)]
void WriteParticlesToBoxes(uint3 threadID : SV_DispatchThreadID)
{
	float3 worldPos = particlesIn[threadID.x].position;
	int3 boxPos = WorldPosToBoxPos(worldPos);
	int boxId = CalcBoxIndex(boxPos);

	int storageIndex = 0;
	InterlockedAdd(particlesBoxesOut[boxId].count, 1, storageIndex);
	
	if (storageIndex < particlesPerBox)
	{
		particlesBoxesOut[boxId].positions[storageIndex] = worldPos;
		particlesBoxesOut[boxId].densities[storageIndex] = particlesIn[threadID.x].density; 
		particlesBoxesOut[boxId].velocities[storageIndex] = particlesIn[threadID.x].velocity;
	}
}

float GetLinearWeight(float dist, float h, float val)
{
	float lerpVal = max((h - dist) / h, 0.0f);
	return lerp(val, 0.0f, lerpVal);
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

[numthreads(8,1,1)]
void UpdateParticlePositions(uint3 threadID : SV_DispatchThreadID)
{
	float3 particleBoundary = simulationHalfSize - (simulationHalfSize * 2.0f / gridAxisSize);
	float restitution = 1.0f;

	float timestep = 0.001f;// timestepInYears * 100.0f;

	float bounce = -0.0f;

	float3 myPos = particlesIn[threadID.x].position;

	float3 CoG = 0.0;

	float boxSize = simulationSize.x / boxesPerAxis.x;

	float3 delta = CoG - myPos;
	delta /= length(delta);
	delta /= dot(delta, delta);

	float3 worldPos = particlesIn[threadID.x].position;
	int3 boxPos = WorldPosToBoxPos(worldPos);

	float3 gravity = float3(0.0f, -10.0f, 0.0f);
	float3 acceleration = gravity;

	float localDensity = 0.0f;

	float3 aggregateVelocity = 0.0f;
	float aggregateCount = 0.0f;

	//for (int dx = -1; dx < 2; dx++)
	{
		for (int dy = -1; dy < 2; dy++)
		{
			//for (int dz = -1; dz < 2; dz++)
			{
				int3 otherBoxPos = boxPos + int3(0, dy, 0);
				
				int otherBoxId = CalcBoxIndex(otherBoxPos);

				int count = particleBoxesIn[otherBoxId].count;

				float amplification = 1.0f;

				if (count > particlesPerBox)
				{
					amplification = (float)count / (float)particlesPerBox;
					count = particlesPerBox;
				}

				for (int posId = 0; posId < count; posId++)
				{
					float3 otherPos = particleBoxesIn[otherBoxId].positions[posId];
					float otherDensity = particleBoxesIn[otherBoxId].densities[posId];
					
					aggregateVelocity += particleBoxesIn[otherBoxId].velocities[posId] * amplification;
					aggregateCount += amplification;

					float3 delta = worldPos - otherPos;
					float dist = length(delta);
					float boxSize = simulationSize.x / boxesPerAxis.x;
					if (dist > 0.0f)
					{
						if (dist < 0.1f)
						{
							dist = 0.1f;
						}
						float smoothLength = 0.9 * boxSize;
						//float pressure = amplification * GetSmoothedWeight(dist, smoothLength) * pow(otherDensity, 7) * 50.0f;
						//float pressure = GetLinearWeight(dist, smoothLength, pow(otherDensity, 7)) * 5000.0f * amplification;
						float pressure = GetLinearWeight(dist, smoothLength, 1.0f) * 0.01f * amplification;

						acceleration += (delta / dist) * pow(pressure, 7.0f) * 10000.0f;
						//localDensity += 0.05f * GetSmoothedWeight(dist, smoothLength) * amplification;
						localDensity += 0.001f * GetLinearWeight(dist, smoothLength, 1.000f) *amplification;
					}
				}
			}
		}
	}

	float3 velocity = (particlesIn[threadID.x].velocity * restitution) + (acceleration * timestep);

	aggregateVelocity /= aggregateCount;
	
	velocity *= (1.0f - viscosity);
	velocity += aggregateVelocity * viscosity;

	float3 newPos = worldPos + (velocity * timestep * 0.5f);
	newPos.x = 0.0f;
	newPos.z = 0.0f;
	
	float3 resetPos = particleBoundary;// -(simulationHalfSize * 3.0f / gridAxisSize);
	if (newPos.x > particleBoundary.x && velocity.x > 0.0f)
	{
		newPos.x = resetPos.x;
		velocity.x *= bounce;
	}
	if (newPos.x < -particleBoundary.x && velocity.x <  0.0f)
	{
		newPos.x = -resetPos.x;
		velocity.x *= bounce;
	}
	if (newPos.y > particleBoundary.y && velocity.y >  0.0f)
	{
		newPos.y = resetPos.y;
		velocity.y *= bounce;
	}
	if (newPos.y < -particleBoundary.y && velocity.y <  0.0f)
	{
		newPos.y = -resetPos.y;
		velocity.y *= bounce;
	}
	if (newPos.z > particleBoundary.z && velocity.z >  0.0f)
	{
		newPos.z = resetPos.z;
		velocity.z *= bounce;
	}
	if (newPos.z < -particleBoundary.z && velocity.z <  0.0f)
	{
		newPos.z = -resetPos.z;
		velocity.z *= bounce;
	}
	
	particlesOut[threadID.x].position = newPos;
	particlesOut[threadID.x].velocity = velocity;
	particlesOut[threadID.x].density = localDensity;
}