﻿using System;
using ImageFramework.Utility;

namespace ImageFramework.Model.Shader
{
    public struct DefaultStatistics
    {
        // luminance: http://homepages.inf.ed.ac.uk/rbf/CVonline/LOCAL_COPIES/POYNTON1/ColorFAQ.html#RTFToC3
        public float Luminance;
        // luma: http://homepages.inf.ed.ac.uk/rbf/CVonline/LOCAL_COPIES/POYNTON1/ColorFAQ.html#RTFToC11
        public float Luma;
        // CIELAB lightness (0-100): http://homepages.inf.ed.ac.uk/rbf/CVonline/LOCAL_COPIES/POYNTON1/ColorFAQ.html#RTFToC4
        public float Lightness;
        public float Alpha;
        // HSV saturation
        public float Saturation;
    }

    internal class DefaultStatisticsShader : StatisticsShader<DefaultStatistics>
    {
        protected override string GetFunctions()
        {
            return Utility.Utility.ToSrgbFunction();
        }

        protected override string GetOneTimeModifyFunction()
        {

            return @"
float4 res;
// linear luminance
res.r = dot(a.rgb, float3(0.2125, 0.7154, 0.0721));
// (video) luma (sRGB). Uses NTSC luminance vector
res.g = dot(toSrgb(a).rgb, float3(0.299, 0.587, 0.114));
// saturation
float sMax = clamp(max(max(a.r, a.g), a.b), 0.0, 1.0);
float sMin = clamp(min(min(a.r, a.g), a.b), 0.0, 1.0);
res.b = sMax == 0.0 ? 0.0 : (sMax - sMin) / sMax;
// keep alpha
res.a = a.a;
return res;
";
        }

        protected override DefaultStatistics GetResult(Color color, int nPixels)
        {
            return new DefaultStatistics
            {
                Luminance = color.Red,
                Lightness = Math.Max((float)(116.0 * Math.Pow(color.Red, 1.0f / 3.0f) - 16.0), 0.0f),
                Luma = color.Green,
                Saturation = color.Blue,
                Alpha = color.Alpha
            };
        }
    }

    internal class MinDefaultStatsticsShader : DefaultStatisticsShader
    {
        internal MinDefaultStatsticsShader()
        {
            Init("MinStatisticsShader");
        }
        protected override string GetCombineFunction()
        {
            return "return min(a, b);";
        }
    }

    internal class MaxDefaultStatsticsShader : DefaultStatisticsShader
    {
        internal MaxDefaultStatsticsShader()
        {
            Init("MaxStatisticsShader");
        }
        protected override string GetCombineFunction()
        {
            return "return max(a, b);";
        }
    }

    internal class AvgDefaultStatisticsShader : DefaultStatisticsShader
    {
        internal AvgDefaultStatisticsShader()
        {
            Init("AvgStatisticsShader");
        }
        protected override DefaultStatistics GetResult(Color color, int nPixels)
        {
            // divide through number of pixels to get average
            float invPixel = 1.0f / nPixels;
            color.Red *= invPixel;
            color.Green *= invPixel;
            color.Blue *= invPixel;
            color.Alpha *= invPixel;

            return base.GetResult(color, nPixels);
        }

        protected override string GetCombineFunction()
        {
            return "return a + b;";
        }
    }
}
