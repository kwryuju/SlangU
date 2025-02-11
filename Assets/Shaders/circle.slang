// from : https://shader-slang.org/slang-playground/

typealias vec2 = float2;
typealias vec3 = float3;
typealias vec4 = float4;

RWTexture2D<float4> result;
uniform float4 _ScreenSize;
uniform float4 _Time;

[shader("compute")]
[numthreads(32,32,1)]
void imageMain(uint3 threadId : SV_DispatchThreadID)
{
    vec2 p = (threadId.xy * 2.0f - _ScreenSize.xy) / (float)_ScreenSize.y;
    float tau = float.getPi() * 2.0;
    float a = atan2(p.x,p.y);
    float r = length(p)*0.75;
    vec2 uv = vec2(a/tau,r);

    float time = _Time.y;

    //get the color
    float xCol = (uv.x - time/3) * 3.0;
    xCol = fmod(abs(xCol), 3.0f);
    vec3 horColour = vec3(0.25, 0.25, 0.25);
    
    if (xCol < 1.0)
    {
        horColour.r += 1.0 - xCol;
        horColour.g += xCol;
    }
    else if (xCol < 2.0)
    {
        xCol -= 1.0;
        horColour.g += 1.0 - xCol;
        horColour.b += xCol;
    }
    else
    {
        xCol -= 2.0;
        horColour.b += 1.0 - xCol;
        horColour.r += xCol;
    }

    // draw color beam
    uv = (2.0 * uv) - 1.0;
    float beamWidth = (0.7 + 
        0.5 * cos(uv.x * 10.0 * tau * 0.15 * clamp(floor(5.0 + 10.0 * cos(time)), 0.0, 10.0)))
        * abs(1.0 / (30.0 * uv.y));
    vec3 horBeam = vec3(beamWidth);

    result[threadId.xy] =  vec4(((horBeam) * horColour), 1.0);
}