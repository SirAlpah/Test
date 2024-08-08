using System;
using Artemis.Core.LayerEffects;
using Artemis.Plugins.LayerEffect.FlickeringLights.PropertyGroups;
using SkiaSharp;

namespace Artemis.Plugins.LayerEffect.FlickeringLights
{
    public class FlickeringLightsPluginLayerEffect : LayerEffect<MainPropertyGroup>
    {
        private float _progress;
        private float _alpha;
        private SKPaint _buffer;

        public override void EnableLayerEffect() { }

        public override void DisableLayerEffect() { }

        public override void Update(double deltaTime)
        {
            // Update _progress to control the blend. For example, you might increment it over time
            // _progress = (float)((_progress + deltaTime) % 1.0); // This is just an example
        }
        
        public override void PreProcess(SKCanvas canvas, SKRect renderBounds, SKPaint paint)
        {
            if (_buffer != null)
            {
                // Blend the current paint with the _buffer paint based on _progress
                paint.Color = BlendColors(_buffer.Color, paint.Color, _progress);
            }

            // Apply the alpha
            paint.Color = paint.Color.WithAlpha((byte)(_alpha * 255));
        }

        public override void PostProcess(SKCanvas canvas, SKRect renderBounds, SKPaint paint)
        {
            // Store the current paint for blending in the next frame
            _buffer = new SKPaint(paint);
        }

        private SKColor BlendColors(SKColor color1, SKColor color2, float t)
        {
            byte r = (byte)(color1.Red + t * (color2.Red - color1.Red));
            byte g = (byte)(color1.Green + t * (color2.Green - color1.Green));
            byte b = (byte)(color1.Blue + t * (color2.Blue - color1.Blue));
            byte a = (byte)(color1.Alpha + t * (color2.Alpha - color1.Alpha));
            return new SKColor(r, g, b, a);
        }
    }
}
