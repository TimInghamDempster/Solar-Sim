RWTexture2D<float4> outputImage : register(t#OutputSlot#);

Texture3D<float4> PosMassReadGrid : register(t#GridReadSlot#);
RWTexture3D<float4> PosMassWriteGrid : register(t#GridWriteSlot#);

Texture3D<float4> VelocityReadGrid : register(t#VelocityGridReadSlot#);
RWTexture3D<float4> VelocityWriteGrid : register(t#VelocityGridWriteSlot#);

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
	float mass = IsInsideObstruction(threadID.xyz) ? 0.0f : 1.0f;
	VelocityWriteGrid[threadID] = float4(0.01f, 0.0f, 0.0f, 0.0f);
	PosMassWriteGrid[threadID] = float4(0.0f, 0.0f, 0.0f, mass);
}

[numthreads(#OutputThreads#, #OutputThreads#, #OutputThreads#)]
void UpdateFluid(uint3 threadID : SV_DispatchThreadID)
{
	float4 posMass = PosMassReadGrid[threadID];
	float4 velocity = VelocityReadGrid[threadID];
	
	posMass += velocity;

	float3 totalPos = posMass + threadID;
	float2 obstructionPos = float2(#ObsPos#, #ObsPos#);

	if (IsInsideObstruction(totalPos))
	{
		float2 delta = totalPos.xy - obstructionPos;
		delta = normalize(delta);

		float speedIntoObstruction = dot(velocity, delta);

		float2 speedChange = delta * speedIntoObstruction * 1.1f;
		velocity.xy -= speedChange;
	}

	PosMassWriteGrid[threadID] = posMass;
	VelocityWriteGrid[threadID] = velocity;
}

[numthreads(#OutputThreads#, #OutputThreads#, 1)]
void OutputGrid(uint3 threadID : SV_DispatchThreadID)
{
	uint3 sampleSite = threadID;
	sampleSite.z = #Resolution# / 2;

	float4 massPos = PosMassReadGrid[sampleSite];

	//float4 col = float4(massVel.x + 0.5f, massVel.y + 0.5f, 0.0f, 1.0f);
	//float4 col = pow(massVel.w,5.0f)*10.0f;
	//float4 col = massVel.w / 1.0f;
	//float col = InkReadGrid[sampleSite].w + (massVel.w * 0.1f);

	/*if (massVel.w < 0.333f)
	{
		col.z = massVel.w * 3.0f;
	}
	else if (massVel.w < 0.666f && massVel.w >= 0.333f)
	{
		col.y = (massVel.w - 0.333f) * 3.0f;
	}
	else if (massVel.w >= 0.666f)
	{
		col.x = (massVel.w - 0.666f) * 3.0f;
	}*/

	float scale = 10.0f;
	float2 scaledAndOffSetPos = (threadID.xy + massPos.xy) * scale;

	outputImage[scaledAndOffSetPos] = massPos.w;// IsFinalMassVelError(massVel) ? float4(0.5f, 0.0f, 0.0f, 1.0f) : col;
}
