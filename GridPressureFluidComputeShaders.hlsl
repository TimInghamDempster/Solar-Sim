RWTexture2D<float4> outputImage : register(t0);

struct Particle
{
	float3 position;
	float3 velocity;
	float3 colour;
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

[numthreads(16, 1, 1)]
void OutputParticlePoints(uint3 threadID : SV_DispatchThreadID)
{
	int3 pos = particlesIn[threadID.x].position * int3(5, 5, 1) + int3(360, 360, 0);
	float3 col = particlesIn[threadID.x].colour;

	float boxHalfSize = simulationHalfSize / gridAxisSize;

	if (pos.z < boxHalfSize && pos.z > -boxHalfSize)
	{
		outputImage[pos.xy] = float4(col.x, col.y,col.z,1);
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

	pressure /= 1.0f;

	outputImage[boxIndex.xy] = log(pressure)/ 15.0f;
}

[numthreads(8,8,8)]
void CalculatePressureGradients(uint3 threadID : SV_DispatchThreadID)
{
	int3 minXLoc = threadID;
	minXLoc.x -= 1;
	float pressureXMin = pressureView[minXLoc];
	if (minXLoc.x < 0)
	{
		pressureXMin = 10000;
	}
	int3 maxXLoc = threadID;
	maxXLoc.x += 1;
	float pressureXMax = pressureView[maxXLoc];
	if (maxXLoc.x >= gridAxisSize)
	{
		pressureXMax = 10000;
	}

	int3 minYLoc = threadID;
	minYLoc.y -= 1;
	float pressureYMin = pressureView[minYLoc];
	int3 maxYLoc = threadID;
	maxYLoc.y += 1;
	float pressureYMax = pressureView[maxYLoc];
	if (minYLoc.y < 0)
	{
		pressureYMin = 10000;
	}
	if (maxYLoc.y >= gridAxisSize)
	{
		pressureYMax = 10000;
	}

	int3 minZLoc = threadID;
	minZLoc.z -= 1;
	float pressureZMin = pressureView[minZLoc];
	int3 maxZLoc = threadID;
	maxZLoc.z += 1;
	float pressureZMax = pressureView[maxXLoc];
	if (minZLoc.z < 0)
	{
		pressureZMin = 10000;
	}
	if (maxZLoc.z >= gridAxisSize)
	{
		pressureZMax = 10000;
	}

	float4 gradient = float4(0.0f, 0.0f, 0.0f, 1.0f);
	gradient.x = pressureXMin - pressureXMax;
	gradient.y = pressureYMin - pressureYMax;
	gradient.z = pressureZMin - pressureZMax;

	pressureGradient[threadID] = gradient;
}

[numthreads(16,1,1)]
void UpdateParticelPositions(uint3 threadID : SV_DispatchThreadID)
{
	float particleBoundary = simulationHalfSize;
	float restitution = 0.9;

	float bounce = -1.0f;

	float3 myPos = particlesIn[threadID.x].position;

	float3 CoG = 0.0;

	float3 delta = CoG - myPos;
	delta /= length(delta);
	delta /= dot(delta, delta);

	float3 pressureGradientIndex = (myPos +  simulationHalfSize) / simulationSize;
	
	//float3 acceleration = (delta * 0.0 + pressureGradientView.SampleLevel(pressureSampler, pressureGradientIndex, 0) * 0.1f)*500000.0;
	float3 acceleration = (delta * 0.0 + pressureGradientView.SampleLevel(pressureSampler, pressureGradientIndex, 0) * 0.1f)*1000000.0;

	float3 velocity = (particlesIn[threadID.x].velocity * restitution) + (acceleration * timestepInYears);

	float3 newPos = particlesIn[threadID.x].position + (velocity * timestepInYears);
	
	/*float resetPos = particleBoundary - (simulationHalfSize * 3.0f / gridAxisSize);
	if (newPos.x > particleBoundary && velocity.x > 0.0f)
	{
		newPos.x = resetPos;
		velocity.x *= bounce;
	}
	if (newPos.x < -particleBoundary && velocity.x <  0.0f)
	{
		newPos.x = -resetPos;
		velocity.x *= bounce;
	}
	if (newPos.y > particleBoundary && velocity.y >  0.0f)
	{
		newPos.y = resetPos;
		velocity.y *= bounce;
	}
	if (newPos.y < -particleBoundary && velocity.y <  0.0f)
	{
		newPos.y = -resetPos;
		velocity.y *= bounce;
	}
	if (newPos.z > particleBoundary && velocity.z >  0.0f)
	{
		newPos.z = resetPos;
		velocity.z *= bounce;
	}
	if (newPos.z < -particleBoundary && velocity.z <  0.0f)
	{
		newPos.z = -resetPos;
		velocity.z *= bounce;
	}*/

	int3 index = (newPos + simulationHalfSize) / simulationSize * gridAxisSize;
	index = clamp(index, 0, gridAxisSize);

	InterlockedAdd(pressureGrid[index], 1);

	particlesOut[threadID.x].position = newPos;
	particlesOut[threadID.x].velocity = velocity;
}