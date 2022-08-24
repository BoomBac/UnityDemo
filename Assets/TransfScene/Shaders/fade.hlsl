#ifndef __FADE_INC__
#define __FADE_INC__

float4 _RadialParam;
int		_Method;
float _BlenderFactor;
sampler2D _NoiseTexture;


#define _Center  _RadialParam.xy
#define _Radius  _RadialParam.z
#define _Density _RadialParam.w
#define _AspectCorrect 1		//使用圆环渐变时，是否进行屏幕比例校正，默认1为校正

#define AlphaLerp(var)			var & 00000001
#define RadialGradient(var)		var & 00000010


float ExpDensity(float depth,float density)
{
	float v2 = depth * density;
	v2 *= v2;
	v2 = 1 / pow(2.718,v2);
	if(depth <= 0)
	v2 = 1;
	return v2;
}
float RadiusGradient(float2 uv)
{
	float aspect = lerp(1,_ScreenParams.x / _ScreenParams.y,_AspectCorrect);
	float2 center = _Center;
	center.x *= aspect;
	uv.x *= aspect;
	float dis = distance(uv,center);
	dis /= _Radius;
	dis = ExpDensity(1 - dis,_Density);
	dis = 1 - dis;
	return saturate(dis);
}

void CalculateFadeAlpha(float2 uv,inout float alpha)
{
	uv += tex2D(_NoiseTexture,uv * 4).xy * 0.1;
	if(AlphaLerp(_Method))
	{
		alpha = _BlenderFactor;
	}
	else if(RadialGradient(_Method))
	{
		alpha = RadiusGradient(uv);
	}
}

#endif //__FADE_INC__