using System;

namespace MoreLocales.Utilities
{
    internal static class MiscHelper
    {
        // lol. xd, even.
        public static Color LerpMany(float amount, ReadOnlySpan<Color> values)
        {
            float p = MathHelper.Clamp(amount * (values.Length - 1), 0, values.Length - 1);
            int start = (int)p;
            int end = Math.Min(start + 1, values.Length - 1);
            return Color.Lerp(values[start], values[end], p - start);
        }
        public static void Begin(this SpriteBatch spriteBatch, SpriteBatchData spriteBatchData)
        {
            spriteBatch.Begin
            (
                spriteBatchData.SortMode, spriteBatchData.BlendState, spriteBatchData.SamplerState, spriteBatchData.DepthStencilState,
                spriteBatchData.RasterizerState, spriteBatchData.Effect, spriteBatchData.Matrix
            );
        }
        public static void End(this SpriteBatch spriteBatch, out SpriteBatchData spriteBatchData)
        {
            spriteBatchData = new SpriteBatchData(spriteBatch);
            spriteBatch.End();
        }
        public struct SpriteBatchData
        {
            public SpriteSortMode SortMode;
            public BlendState BlendState;
            public SamplerState SamplerState;
            public DepthStencilState DepthStencilState;
            public RasterizerState RasterizerState;
            public Effect Effect;
            public Matrix Matrix;
            public SpriteBatchData(SpriteBatch spriteBatch)
            {
                if (spriteBatch is null)
                    return;

                SortMode = spriteBatch.sortMode;
                BlendState = spriteBatch.blendState;
                SamplerState = spriteBatch.samplerState;
                DepthStencilState = spriteBatch.depthStencilState;
                RasterizerState = spriteBatch.rasterizerState;
                Effect = spriteBatch.customEffect;
                Matrix = spriteBatch.transformMatrix;
            }
        }
    }
}
