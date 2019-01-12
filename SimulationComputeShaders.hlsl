RWTexture2D<float> outputImage;
//Texture2D OldVoltages;

[numthreads(32, 32, 1)]
void OutputParticlePoints( uint3 threadID : SV_DispatchThreadID )
{
	float dist = sqrt(threadID.x*threadID.x + threadID.y*threadID.y);
	if (dist > 500)
	{
		outputImage[threadID.xy] = 155;
	}
}