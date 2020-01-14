RWTexture2D<float4> outputImage : register(t#OutputSlot#);

/*Texture3D<float4> ReadGrid : register(t#GridReadSlot#);
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
	}*/

	/*if (location.x == 128 && location.y == 128 && location.z == 128)
	{
		return float4(0.0f, 0.0f, 0.0f, 0.9f);
	}*/
	/*return currentMassVel;
}*/

[numthreads(#OutputThreads#, #OutputThreads#, 1)]
void OutputGrid(uint3 threadID : SV_DispatchThreadID)
{
	uint3 sampleSite = threadID;
	sampleSite.z = #Resolution# / 2;

	//float4 massVel = ReadGrid[sampleSite];

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

	outputImage[threadID.xy] = 0.5f;// IsFinalMassVelError(massVel) ? float4(0.5f, 0.0f, 0.0f, 1.0f) : col;
}
