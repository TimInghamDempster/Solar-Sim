RWTexture2D<float4> outputImage : register(t#OutputSlot#);

Texture3D<float4> ReadGrid : register(t#GridReadSlot#);
RWTexture3D<float4> WriteGrid : register(t#GridWriteSlot#);

float4 ApplyBoundaryConditions(uint3 location, float4 currentMassVel)
{
	
	if (location.x == 0)
	{
		return float4(0.4f, 0.3f, 0.0f, 0.5f);
	}
	else if (location.x == #Resolution# - 1 ||
			 location.y == 0 ||
			 location.y == #Resolution#  - 1 ||
			 location.z == 0 ||
			 location.z == #Resolution#  - 1)
	{
		return float4(0.4f, 0.3f, 0.0f, 0.5f);
	}
	else
	{
		return currentMassVel;
	}
}

float ObstructionFlux(int3 location)
{
	if (location.x > 124 &&
		location.x < 132 &&
		location.y > 124 &&
		location.y < 132)
	{
		return 0.0f;
	}

	return 1.0f;
}

float CalcFlux(float4 insideMasVel, float4 outsideMasVel, float3 fluxAxisOutward)
{
	float3 boundaryVelocity = (insideMasVel.xyz + outsideMasVel.xyz) / 2.0f;

	float fluxIn = max(dot(boundaryVelocity,-fluxAxisOutward), 0.0f) * outsideMasVel.w;
	float fluxOut =max(dot(boundaryVelocity, fluxAxisOutward), 0.0f) * insideMasVel.w;

	return  fluxIn - fluxOut;
}

float4 ApplyTransport(uint3 location)
{
	float4 masVel = ReadGrid[location];

	float3 dir[6] =
	{
		float3( 1.0f, 0.0f, 0.0f),
		float3(-1.0f, 0.0f, 0.0f),
		float3( 0.0f, 1.0f, 0.0f),
		float3( 0.0f,-1.0f, 0.0f),
		float3( 0.0f, 0.0f, 1.0f),
		float3( 0.0f, 0.0f,-1.0f)
	};
	float totalMass = masVel.w;

	// We need to know how much of the original mass is left
	// to work out the new average velocity of the cell
	float massAfterOutflow = totalMass;

	// We also need a count of the total momentum that flows into
	// the cell plus the momentum of the mass  left after outflow
	// to work out the new average velocity
	float3 totalCellMomentum = float3(0.0f, 0.0f, 0.0f);

	for (int i = 0; i < 6; i++)
	{
		float4 neighbourMasVel = ReadGrid[location + dir[i]];
		
		float flux = CalcFlux(masVel, neighbourMasVel, dir[i]) * ObstructionFlux(location) * ObstructionFlux(location + dir[i]);

		totalMass += flux;
		massAfterOutflow += min(flux, 0.0f); // This variable needs to know how much mass left the cell
		totalCellMomentum += max(flux * neighbourMasVel.xyz, 0.0f); // How much momentum came into the cell?  Can't be negative*/
	}

	totalCellMomentum += masVel.xyz * massAfterOutflow;

	float4 newMassVel = totalMass;
	newMassVel.xyz = totalCellMomentum / max(totalMass, 0.000001f);

	return newMassVel;
}

[numthreads(#OutputThreads#, #OutputThreads#, #OutputThreads#)]
void GridFluidMain(uint3 threadID : SV_DispatchThreadID)
{
	float4 massVel = ApplyTransport(threadID);

	massVel = ApplyBoundaryConditions(threadID, massVel);

	WriteGrid[threadID] = massVel;
}

[numthreads(#OutputThreads#, #OutputThreads#, 1)]
void OutputGrid(uint3 threadID : SV_DispatchThreadID)
{
	uint3 sampleSite = threadID;
	sampleSite.z = #Resolution# / 2;

	float4 massVel = ReadGrid[sampleSite];

	outputImage[threadID.xy] = massVel.w >= 0 ? massVel.w : float4(0.5f, 0.0f, 0.0f, 1.0f);
}