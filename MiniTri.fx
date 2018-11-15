float2 viewPos;
float2 viewScale;

Texture2D txGate : register(t0);
SamplerState txSampler : register (s0)
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
} ;

Texture2D txVoltages : register(t1);

struct Wire_VS_IN
{
	float4 pos : POSITION;
	uint input : TEXCOORD0;
};

struct Wire_PS_IN
{
	float4 pos : SV_POSITION;
};

struct Gate_VS_IN
{
	float4 pos : POSITION;
	float4 txPos : TEXCOORD0;
};

struct Gate_PS_IN
{
	float4 pos : SV_POSITION;
	float4 txPos :TEXCOORD0;
};

Wire_PS_IN Wire_VS( Wire_VS_IN input )
{
	Wire_PS_IN output = (Wire_PS_IN)0;
	
	output.pos = input.pos;
	output.pos.xy += viewPos;
	output.pos.xy *= viewScale;

	return output;
}

float4 Wire_PS( Wire_PS_IN input ) : SV_Target
{
	return txVoltages.Load(0,0);
}

Gate_PS_IN Gate_VS( Gate_VS_IN input )
{
	Gate_PS_IN output = (Gate_PS_IN)0;
	
	output.pos = input.pos;
	output.pos.xy += viewPos;
	output.pos.xy *= viewScale;
	output.txPos = input.txPos;
	
	return output;
}

float4 Gate_PS( Gate_PS_IN input ) : SV_Target
{
	return txGate.Sample( txSampler, input.txPos.xy);
}

technique11 RenderWires
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_5_0, Wire_VS() ) );
		SetPixelShader( CompileShader( ps_5_0, Wire_PS() ) );
	}
}

technique11 RenderGates
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_5_0, Gate_VS() ) );
		SetPixelShader( CompileShader( ps_5_0, Gate_PS() ) );
	}
}