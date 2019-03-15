RWTexture2D<float4> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
};

SamplerState pressureSampler : register (s0)
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

StructuredBuffer<Particle> particlesIn : register(t2);
RWStructuredBuffer<Particle> particlesOut : register(t0);
RWTexture3D<uint> pressureGrid : register(t1);
Texture3D<uint> pressureView : register(t1);
RWTexture3D<float4> pressureGradient : register(t0);
Texture3D<float4> pressureGradientView : register(t3);

cbuffer physicsConstants : register(c0)
{
	float timestepInYears;
	float simulationHalfSize;
	float simulationSize;
	float gridAxisSize;
};

cbuffer displacer : register(c1)
{
	float4 displacerPosition;
}

[numthreads(16,1,1)]
void ApplyDisplacementForce(uint3 threadID : SV_DispatchThreadID)
{
	float3 myPos = particlesIn[threadID.x].position;
	
	float3 acceleration = 0.0f;

	float3 delta = myPos - displacerPosition;
	float dist = length(delta);
	if (dist < (simulationSize / gridAxisSize) * 10.0)
	{
		acceleration = delta * 10000000.0f / dist;
	}

	float3 velocity = particlesIn[threadID.x].velocity + acceleration * timestepInYears;

	particlesOut[threadID.x].position = myPos;
	particlesOut[threadID.x].velocity = velocity;
}

[numthreads(8, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_DispatchThreadID)
{
	int3 pos = particlesIn[threadID.x].position * int3(6, 6, 1) + int3(640, 360, 0);

	float boxHalfSize = simulationHalfSize / gridAxisSize;

	if (pos.z < boxHalfSize && pos.z > -boxHalfSize)
	{
		outputImage[pos.xy] = 0.9f;
	}
}

[numthreads(8,8,1)]
void OutputPressures(uint3 threadId : SV_DispatchThreadID)
{
	uint3 boxIndex = threadId;
	boxIndex.z = gridAxisSize / 2.0f;

	float pressure = pressureView[boxIndex] * 2;

	uint3 leftBoxId = boxIndex;
	leftBoxId.x -= 1;
	pressure += pressureView[leftBoxId];

	uint3 rightBoxId = boxIndex;
	rightBoxId.x += 1;
	pressure += pressureView[rightBoxId];

	uint3 lowerBoxId = boxIndex;
	lowerBoxId.y -= 1;
	pressure += pressureView[lowerBoxId];

	uint3 upperBoxId = boxIndex;
	upperBoxId.y += 1;
	pressure += pressureView[upperBoxId];

	pressure /= 6.0f;

	outputImage[boxIndex.xy] = pressure / 50.0f;
}

[numthreads(8,8,8)]
void CalculatePressureGradients(uint3 threadID : SV_DispatchThreadID)
{
	int3 minXLoc = threadID;
	minXLoc.x -= 1;
	float pressureXMin = pressureView[minXLoc];
	int3 maxXLoc = threadID;
	maxXLoc.x += 1;
	float pressureXMax = pressureView[maxXLoc];

	int3 minYLoc = threadID;
	minYLoc.y -= 1;
	float pressureYMin = pressureView[minYLoc];
	int3 maxYLoc = threadID;
	maxYLoc.y += 1;
	float pressureYMax = pressureView[maxYLoc];

	int3 minZLoc = threadID;
	minZLoc.z -= 1;
	float pressureZMin = pressureView[minZLoc];
	int3 maxZLoc = threadID;
	maxZLoc.z += 1;
	float pressureZMax = pressureView[maxXLoc];

	float4 gradient = float4(0.0f, 0.0f, 0.0f, 1.0f);
	gradient.x = pressureXMin - pressureXMax;
	gradient.y = pressureYMin - pressureYMax;
	gradient.z = pressureZMin - pressureZMax;

	pressureGradient[threadID] = gradient;
}

[numthreads(16,1,1)]
void UpdateParticelPositions(uint3 threadID : SV_DispatchThreadID)
{
	float particleBoundary = simulationHalfSize * 1.0f;

	float3 myPos = particlesIn[threadID.x].position;

	float3 pressureGradientIndex = (myPos +  simulationHalfSize) / simulationSize;
	
	float3 acceleration = pressureGradientView.SampleLevel(pressureSampler, pressureGradientIndex, 0) * 100000.0f;

	float3 velocity = particlesIn[threadID.x].velocity + acceleration * timestepInYears;

	float3 newPos = particlesIn[threadID.x].position + (velocity * timestepInYears);
	
	if (newPos.x > particleBoundary && velocity.x > 0)
	{
		newPos.x = particleBoundary;
		velocity.x *= -1.0f;
	}
	if (newPos.x < -particleBoundary && velocity.x < 0)
	{
		newPos.x = -particleBoundary;
		velocity.x *= -1.0f;
	}
	if (newPos.y > particleBoundary && velocity.y > 0)
	{
		newPos.y = particleBoundary;
		velocity.y *= -1.0f;
	}
	if (newPos.y < -particleBoundary && velocity.y < 0)
	{
		newPos.y = -particleBoundary;
		velocity.y *= -1.0f;
	}
	if (newPos.z > particleBoundary && velocity.z > 0)
	{
		newPos.z = particleBoundary;
		velocity.z *= -1.0f;
	}
	if (newPos.z < -particleBoundary && velocity.z < 0)
	{
		newPos.z = -particleBoundary;
		velocity.z *= -1.0f;
	}

	int3 index = (newPos + simulationHalfSize) / simulationSize * gridAxisSize;
	InterlockedAdd(pressureGrid[index], 1);

	particlesOut[threadID.x].position = newPos;
	particlesOut[threadID.x].velocity = velocity;
}