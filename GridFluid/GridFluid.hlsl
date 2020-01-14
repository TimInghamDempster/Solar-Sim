RWTexture2D<float4> outputImage : register(t#OutputSlot#);

Texture3D<float4> ReadGrid : register(t#GridReadSlot#);
RWTexture3D<float4> WriteGrid : register(t#GridWriteSlot#);

Texture3D<float4> InkReadGrid : register(t#InkReadSlot#);
RWTexture3D<float4> InkWriteGrid : register(t#InkWriteSlot#);

float4 ApplyBoundaryConditions(uint3 location, float4 currentMassVel)
{

	if (location.x < 2)
	{
		if (location.y % 10 == 0)
		{
			InkWriteGrid[location] = 0.0f;
		}
		else
		{
			InkWriteGrid[location] = 1.0f;
		}
		return float4(0.03f, 0.0f, 0.0f, 0.1f);
	}

	if (location.x > #Resolution#  - 2 ||
		location.y < 2 ||
		location.y > #Resolution#   - 2 ||
		location.z < 2 ||
		location.z > #Resolution#    - 2)
	{
		return float4(currentMassVel.x, currentMassVel.y, currentMassVel.z, 0.1f);
	}

	/*if (location.x == 128 && location.y == 128 && location.z == 128)
	{
		return float4(0.0f, 0.0f, 0.0f, 0.9f);
	}*/
	return currentMassVel;
}

float ObstructionFlux(int3 location)
{
	int deltaX = location.x - 64;
	int deltaY = location.y - 128;

	if ((deltaX * deltaX) + (deltaY * deltaY) < 64)
	{
		return 0.0f;
	}

	return 1.0f;
}

// Assumes fluxAxisOutward is normalised
float CalcFlux(float4 insideMasVel, float4 outsideMasVel, float3 fluxAxisOutward)
{
	float3 boundaryVelocity = ((insideMasVel.xyz * insideMasVel.w) + (outsideMasVel.xyz * outsideMasVel.w)) / 2.0f;

	float fluxIn = max(dot(boundaryVelocity.xyz, -fluxAxisOutward), 0.0f);
	float fluxOut = max(dot(boundaryVelocity.xyz, fluxAxisOutward), 0.0f);

	return clamp(fluxIn - fluxOut, -0.16, 0.16);
}

float4 ApplyTransport(uint3 location)
{
	float4 massVel = ReadGrid[location];

	float3 dir[6] =
	{
		float3( 1.0f, 0.0f, 0.0f),
		float3(-1.0f, 0.0f, 0.0f),
		float3( 0.0f, 1.0f, 0.0f),
		float3( 0.0f,-1.0f, 0.0f),
		float3( 0.0f, 0.0f, 1.0f),
		float3( 0.0f, 0.0f,-1.0f)
	};
	float totalMass = massVel.w;

	float oldInk = InkReadGrid[location];
	float totalInk = oldInk;

	// We need to know how much of the original mass is left
	// to work out the new average velocity of the cell
	float massAfterOutflow = totalMass;

	// We also need a count of the total momentum that flows into
	// the cell plus the momentum of the mass  left after outflow
	// to work out the new average velocity
	float3 totalCellMomentum = float3(0.0f, 0.0f, 0.0f);

	float slipVel = 1.0f;

	//float4 shear = 0.0f;

	const float viscosity = 0.0f;

	for (int i = 0; i < 6; i++)
	{
		float4 neighbourMasVel = ReadGrid[location + dir[i]];
		float neighbourInk = InkReadGrid[location + dir[i]];
		
		float flux = CalcFlux(massVel, neighbourMasVel, dir[i]) * ObstructionFlux(location) * ObstructionFlux(location + dir[i]);

		totalMass += flux;
		massAfterOutflow += min(flux, 0.0f); // This variable needs to know how much mass left the cell
		totalCellMomentum += max(flux, 0.0f) * neighbourMasVel.xyz; // How much momentum came into the cell?  Can't be negative*/

		slipVel *= ObstructionFlux(location + dir[i]);

		totalInk += max(flux, 0.0f) * neighbourInk;
		totalInk += min(flux, 0.0f) * oldInk;

		//float4 thisShear = neighbourMasVel;
		//thisShear.xyz *= thisShear.w;

		//shear += thisShear;
	}

	massAfterOutflow = max(massAfterOutflow, 0.0f);

	totalCellMomentum += massVel.xyz * massAfterOutflow;

	float4 newMassVel = 0.0f;
	newMassVel.w = max(totalMass, 0.00001f);
	newMassVel.xyz = (totalCellMomentum / max(totalMass, 0.00001f)) * slipVel;// *(1.0f - viscosity);
	//massVel.xyz += (shear.xyz / shear.w) * viscosity;

	InkWriteGrid[location] = totalInk;

	return newMassVel;
}

float4 ApplyPressure(uint3 location)
{
	float4 oldMassVel = ReadGrid[location];

	float3 dir[6] =
	{
		float3(1.0f, 0.0f, 0.0f),
		float3(-1.0f, 0.0f, 0.0f),
		float3(0.0f, 1.0f, 0.0f),
		float3(0.0f,-1.0f, 0.0f),
		float3(0.0f, 0.0f, 1.0f),
		float3(0.0f, 0.0f,-1.0f)
	};

	float pressureConstant = 0.0001f;
	const float stateExponent = 3.0f;
	const float pressureOffset = 1.0f;
	const float meanDensity = 0.3f;

	for (int i = 0; i < 6; i++)
	{
		float4 neighbourMassVel = ReadGrid[location + dir[i]];
		
		float pressure = max(pow(oldMassVel.w / meanDensity, stateExponent) - pressureOffset, 0);
		float neighbourPressure = max(pow(neighbourMassVel.w / meanDensity, stateExponent) - pressureOffset, 0);
		
		float delta = pressure - neighbourPressure;

		float pressureEffect = delta * pressureConstant * ObstructionFlux(location) * ObstructionFlux(location + dir[i]);

		float3 effect = dir[i] * pressureEffect;
		oldMassVel.xyz += effect;
	}

	float safeVal = 0.16f;
	oldMassVel.x = clamp(oldMassVel.x, -safeVal, safeVal);
	oldMassVel.y = clamp(oldMassVel.y, -safeVal, safeVal);
	oldMassVel.z = 0.0f;// clamp(oldMassVel.z, -safeVal, safeVal);

	return oldMassVel;
}

[numthreads(#OutputThreads#, #OutputThreads#, #OutputThreads#)]
void TransportStep(uint3 threadID : SV_DispatchThreadID)
{
	float4 massVel = ApplyTransport(threadID);
	WriteGrid[threadID] = massVel;
}

[numthreads(#OutputThreads#, #OutputThreads#, #OutputThreads#)]
void PressureStep(uint3 threadID : SV_DispatchThreadID)
{
	float4 massVel = ApplyPressure(threadID);
	massVel = ApplyBoundaryConditions(threadID, massVel);
	WriteGrid[threadID] = massVel;
}

bool IsFinalMassVelError(float4 massVel)
{
	if (isnan(massVel.x))
	{
		return true;
	}
	if (isnan(massVel.y))
	{
		return true;
	}
	if (isnan(massVel.z))
	{
		return true;
	}
	if (isnan(massVel.w))
	{
		return true;
	}
	if (massVel.x >= 1.0f || massVel.x <= -1.0f)
	{
		return true;
	}
	if (massVel.y >= 1.0f || massVel.y <= -1.0f)
	{
		return true;
	}
	if (massVel.z >= 1.0f || massVel.z <= -1.0f)
	{
		return true;
	}
	return false;
}

[numthreads(#OutputThreads#, #OutputThreads#, 1)]
void OutputGrid(uint3 threadID : SV_DispatchThreadID)
{
	uint3 sampleSite = threadID;
	sampleSite.z = #Resolution# / 2;

	float4 massVel = ReadGrid[sampleSite];

	//float4 col = float4(massVel.x + 0.5f, massVel.y + 0.5f, 0.0f, 1.0f);
	//float4 col = pow(massVel.w,5.0f)*10.0f;
	float4 col = massVel.w / 1.0f;
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

	outputImage[threadID.xy] = IsFinalMassVelError(massVel) ?  float4(0.5f, 0.0f, 0.0f, 1.0f) : col;
}
