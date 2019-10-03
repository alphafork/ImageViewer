#setting title, Color Clamping
#setting description, Clamps the RGB values between minimum and maximum. Alpha won't be changed.

#param Minimum, minimum, float, 0
#param Maximum, maximum, float, 1

float4 filter(int2 pixelCoord, int2 size)
{
	float4 color = src_image[pixelCoord];
	return float4(clamp(color.rgb, float3(minimum, minimum, minimum), float3(maximum, maximum, maximum)), color.a);
}