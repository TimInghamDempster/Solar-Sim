RWTexture2D<float4> outputImage : register(t#OutputSlot#);

Texture3D<float4> PosMassReadGrid : register(t#GridReadSlot#);
RWTexture3D<float4> PosMassWriteGrid : register(t#GridWriteSlot#);

Texture3D<float4> VelocityDensityReadGrid : register(t#VelocityGridReadSlot#);
RWTexture3D<float4> VelocityDensityWriteGrid : register(t#VelocityGridWriteSlot#);

/*Texture3D<float4> InkReadGrid : register(t#InkReadSlot#);
RWTexture3D<float4> InkWriteGrid : register(t#InkWriteSlot#);*/

bool IsInsideObstruction(float3 pos)
{
	float2 obstructionPos = float2(#ObsPos#, #ObsPos#);

	float2 delta = pos.xy - obstructionPos;

	return length(delta) < 4.0f;
}

[numthreads(#OutputThreads#, #OutputThreads#, #OutputThreads#)]
void InitialiseFluid(uint3 threadID : SV_DispatchThreadID)
{
	float mass = IsInsideObstruction(threadID.xyz) ? 0.0f : 0.5f;
	VelocityDensityWriteGrid[threadID] = float4(0.1f, 0.0f, 0.0f, mass);
	PosMassWriteGrid[threadID] = float4(0.0f, 0.0f, 0.0f, mass);
}

// Incompressible fluids act as if density is
// very different to compressible fluids,
// implementation of weakly compressible SPH
float CalcPressure(float actualDensity)
{
	bool isGas = true;
	float targetDensity = 0.5f;
	float stiffness = 7.0f;
	float pressureConstant = 0.05f;

	if (isGas)
	{
		return actualDensity * pressureConstant;
	}
	else
	{
		return max(0.0f, pow((actualDensity / targetDensity) - 1.0f, stiffness));
	}
}

[numthreads(#OutputThreads#, #OutputThreads#, #OutputThreads#)]
void UpdateFluid(uint3 threadID : SV_DispatchThreadID)
{
	float timestep = 0.1f;

	float4 posMass = PosMassReadGrid[threadID];
	float4 velocityDensity = VelocityDensityReadGrid[threadID];
	
	posMass.xyz += velocityDensity.xyz * timestep;
	float3 totalPos = posMass + threadID;

	int3 dir[6] =
	{
		int3( 1, 0, 0),
		int3(-1, 0, 0),
		int3( 0, 1, 0),
		int3( 0,-1, 0),
		int3( 0, 0, 1),
		int3( 0, 0,-1)
	};
	float3 neighbourPositions[6];

	float myPressure =
		CalcPressure(velocityDensity.w);

	// Neighbour interaction
	for (int i = 0; i < 6; i++)
	{
		int3 neighbourGridLoc = threadID + dir[i];
		float4 neighbourPosMass = PosMassReadGrid[neighbourGridLoc];
		neighbourPosMass.xyz += neighbourGridLoc;

		float3 delta = totalPos.xyz - neighbourPosMass.xyz;
		float3 neighbourDir = delta / length(delta);

		float neighbourPressure = 
			CalcPressure(
				VelocityDensityReadGrid[neighbourGridLoc].w);

		float densityDiff = myPressure - neighbourPressure;

		float pressureGradient = densityDiff / length(delta);

		float force = pressureGradient;

		velocityDensity.xyz -= force * neighbourDir * timestep;
		velocityDensity.z = 0.0f;

		neighbourPositions[i] = neighbourPosMass.xyz;
	}

	float3 boundingBox = 
		float3(
			neighbourPositions[0].x - neighbourPositions[1].x,
			neighbourPositions[2].y - neighbourPositions[3].y,
			neighbourPositions[4].z - neighbourPositions[5].z);
	
	float volume = boundingBox.x * boundingBox.y * boundingBox.z / 8.0f;
	velocityDensity.w = posMass.w / volume;

	// Handle obstruction
	float2 obstructionPos = float2(#ObsPos#, #ObsPos#);
	if (IsInsideObstruction(totalPos))
	{
		float2 delta = totalPos.xy - obstructionPos;
		delta = normalize(delta);

		float speedIntoObstruction = min(dot(velocityDensity, delta), 0.0f);

		float2 speedChange = delta * speedIntoObstruction * 1.1f;
		velocityDensity.xy = 0.0f;// speedChange;
	}

	PosMassWriteGrid[threadID] = posMass;
	VelocityDensityWriteGrid[threadID] = velocityDensity;
}

[numthreads(#OutputThreads#, #OutputThreads#, 1)]
void OutputGrid(uint3 threadID : SV_DispatchThreadID)
{
	uint3 sampleSite = threadID;
	sampleSite.z = #Resolution# / 2;

	float4 posMass = PosMassReadGrid[sampleSite];
	float4 velocityDensity = VelocityDensityReadGrid[sampleSite];

	float scale = 10.0f;
	float2 scaledAndOffSetPos = ((threadID.xy + posMass.xy) * scale) + 10.5f;
	//outputImage[scaledAndOffSetPos] = IsFinalMassVelError(massVel) ? float4(0.5f, 0.0f, 0.0f, 1.0f) : col;
	outputImage[scaledAndOffSetPos] = velocityDensity.w;
	//outputImage[scaledAndOffSetPos] = posMass.w;
}
