using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;

namespace MoreLocales.Common
{
    /// <summary>
    /// <see href="https://bit.ly/3ZwJJaD"/>
    /// </summary>
    public static class BetterLangMenuV2
    {
        public static LangMenuV2 FinalRender => ModContent.GetInstance<LangMenuV2>();
        private static Asset<Texture2D> _panelTexture;
        private static Asset<Texture2D> _panelHighlight;
        private static Asset<Texture2D> _flagAtlas;
        static BetterLangMenuV2()
        {
            _panelTexture = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel");
            _panelHighlight = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel_Highlight");
            _flagAtlas = ModContent.Request<Texture2D>("MoreLocales/Assets/Flags");
        }
        public static void DrawTopLeft(SpriteBatch sb, int width, int height)
        {
            UIHelper.DrawAdjustableBox(sb, _panelTexture.Value, new Rectangle(0, 0, width, height), Color.White);
        }
    }
    public class LangMenuV2 : ARenderTargetContentByRequest, ILoadable
    {
        public void Load(Mod mod)
        {
            Main.ContentThatNeedsRenderTargets.Add(this);
        }
        public override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            int sizeX = BetterLangMenuUI.ScreenWidth / 3;
            int sizeY = BetterLangMenuUI.ScreenHeight - 480;

            base.PrepareARenderTarget_AndListenToEvents(ref _target, device, sizeX, sizeY, RenderTargetUsage.PlatformContents);

            var oldTargets = device.GetRenderTargets();

            device.Clear(Color.Transparent);
            device.SetRenderTarget(_target);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            BetterLangMenuV2.DrawTopLeft(spriteBatch, sizeX, sizeY);
            spriteBatch.End();

            device.SetRenderTargets(oldTargets);
            _wasPrepared = true;
        }
        public void Unload()
        {
            Main.ContentThatNeedsRenderTargets.Remove(this);
        }
    }
    /// <summary>
    /// Defines the data for a 
    /// </summary>
    public struct LanguageButtonData
    {

    }
}
