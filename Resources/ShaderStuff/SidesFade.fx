sampler uImage0 : register(s0);
float2 uImageSize0;
float uIntensity;

float4 SidesFade(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    
    float pixels = uIntensity;
    
    float x = coords.x * uImageSize0.x;

    float distFromCenter = abs(x - uImageSize0.x * 0.5);

    float fadeStart = uImageSize0.x * 0.5 - pixels;
    float t = (distFromCenter - fadeStart) / pixels;
    float fadeAmount = smoothstep(0.0, 1.0, t);

    color *= 1.0 - fadeAmount;

    return color;
}

technique Technique1
{
    pass ShaderPass
    {
        PixelShader = compile ps_2_0 SidesFade();
    }
}