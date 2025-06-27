using System;
using Terraria;

namespace MoreLocales.Utilities
{
    internal static class UIHelper
    {
        #region stuff
        /// <summary>
        /// Ported from ITD
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="tex"></param>
        /// <param name="rect"></param>
        /// <param name="col"></param>
        public static void DrawAdjustableBox(SpriteBatch spriteBatch, Texture2D tex, Rectangle rect, Color col)
        {
            Vector2 quadSize = new(tex.Width / 3, tex.Height / 3);
            // scales for the extendable bits of the box.
            // as an important note, you should probably try to avoid the corners and sides squashing for real applications,
            // but as a failsafe, i've added these to make sure an adjustablebox never looks weird.
            float cornerScaleX = Math.Min(1, rect.Width / (quadSize.X * 2));
            float cornerScaleY = Math.Min(1, rect.Height / (quadSize.Y * 2));
            float sideScaleX = Math.Max(0, (rect.Width - quadSize.X * 2) / quadSize.X);
            float sideScaleY = Math.Max(0, (rect.Height - quadSize.Y * 2) / quadSize.Y);

            void DrawSegment(Vector2 position, Rectangle frame, Vector2 scale)
            {
                spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            // Draw center

            Rectangle centerFrame = tex.Frame(3, 3, 1, 1);
            DrawSegment(new Vector2(rect.X + quadSize.X * cornerScaleX, rect.Y + quadSize.Y * cornerScaleY), centerFrame, new Vector2(sideScaleX, sideScaleY));

            // Draw sides

            Rectangle topSideFrame = tex.Frame(3, 3, 1, 0);
            DrawSegment(new Vector2(rect.X + quadSize.X * cornerScaleX, rect.Y), topSideFrame, new Vector2(sideScaleX, cornerScaleY));

            Rectangle leftSideFrame = tex.Frame(3, 3, 0, 1);
            DrawSegment(new Vector2(rect.X, rect.Y + quadSize.Y * cornerScaleY), leftSideFrame, new Vector2(cornerScaleX, sideScaleY));

            Rectangle rightSideFrame = tex.Frame(3, 3, 2, 1);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X * cornerScaleX, rect.Y + quadSize.Y * cornerScaleY), rightSideFrame, new Vector2(cornerScaleX, sideScaleY));

            Rectangle bottomSideFrame = tex.Frame(3, 3, 1, 2);
            DrawSegment(new Vector2(rect.X + quadSize.X * cornerScaleX, rect.Y + rect.Height - quadSize.Y * cornerScaleY), bottomSideFrame, new Vector2(sideScaleX, cornerScaleY));

            // Draw corners
            Vector2 cornerScale = new(cornerScaleX, cornerScaleY);

            Rectangle topLeftCorner = tex.Frame(3, 3, 0, 0);
            DrawSegment(new Vector2(rect.X, rect.Y), topLeftCorner, cornerScale);

            Rectangle topRightCorner = tex.Frame(3, 3, 2, 0);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X * cornerScaleX, rect.Y), topRightCorner, cornerScale);

            Rectangle bottomLeftCorner = tex.Frame(3, 3, 0, 2);
            DrawSegment(new Vector2(rect.X, rect.Y + rect.Height - quadSize.Y * cornerScaleY), bottomLeftCorner, cornerScale);

            Rectangle bottomRightCorner = tex.Frame(3, 3, 2, 2);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X * cornerScaleX, rect.Y + rect.Height - quadSize.Y * cornerScaleY), bottomRightCorner, cornerScale);
        }
        #endregion
    }
}
