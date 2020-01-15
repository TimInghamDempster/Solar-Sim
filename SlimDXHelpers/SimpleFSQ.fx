float2 viewPos;
float2 viewScale;

Texture2D<float4> tx : register(t0);
SamplerState txSampler : register (s0)
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
};

struct VS_IN
{
	float4 pos : POSITION;
	float4 txPos : TEXCOORD0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 txPos :TEXCOORD0;
};



PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = input.pos;
	output.txPos = input.txPos;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	return tx.Sample(txSampler, (input.txPos.xy * 1.0));// -float2(0.005f,0.005f));
}

technique11 Render
{
	pass P0
	{
		SetGeometryShader(0);
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}