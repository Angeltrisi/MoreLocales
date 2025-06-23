using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace MoreLocales.Common
{
    public class BetterLangMenuUI : UIState, IHaveBackButtonCommand
    {
        // i was testing different variables from screen dimensions, i'm not that lazy to not want to write Main ok¿¿
        public static int ScreenWidth => Main.screenWidth;
        public static int ScreenHeight => Main.screenHeight;
        public static Vector2 ScreenResolution => new(ScreenWidth, ScreenHeight);
        public UIState PreviousUIState { get; set; }
        public BackButton backButton;
        private Vector2 _previousResolution;
        public override void OnInitialize()
        {
            backButton = new(70f, 50f);
            Append(backButton);
        }
        private void RecalculateButtonPosition(Vector2 newRes)
        {
            if (_previousResolution == newRes)
                return;

            _previousResolution = newRes;

            float screenMiddle = newRes.X * 0.5f;

            Vector2 backButtonDimensions = new(backButton.Width.Pixels, backButton.Height.Pixels);
            float halfX = backButtonDimensions.X * 0.5f;

            backButton.Left.Set(screenMiddle - halfX, 0f);
            backButton.Top.Set(newRes.Y - backButtonDimensions.Y - 30f, 0f);

            Recalculate();
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 newRes = ScreenResolution;

            var render = BetterLangMenuV2.FinalRender;
            render.Request();
            if (render.IsReady)
            {
                Texture2D tex = render._target;
                Vector2 offset = new(0f, 50f);
                Vector2 drawCenter = newRes * 0.5f + offset;
                Vector2 innerSize = tex.Size();

                Rectangle centered = Utils.CenteredRectangle(drawCenter, innerSize);

                Rectangle centeredBig = centered;
                centeredBig.Inflate(BetterLangMenuV2.PaddingXTotal, BetterLangMenuV2.PaddingYTotal);

                UIHelper.DrawAdjustableBox(spriteBatch, BetterLangMenuV2._panelTexture.Value, centeredBig, Color.Gray);

                spriteBatch.End(out var spriteBatchData);
                spriteBatchData.SortMode = SpriteSortMode.Immediate;
                spriteBatch.Begin(spriteBatchData);

                LangMenuV2.sideFadeShader.Apply(tex, 20f);
                spriteBatch.Draw(tex, drawCenter, null, Color.White, 0f, innerSize * 0.5f, 1f, SpriteEffects.None, 0f);

                spriteBatch.End();
                spriteBatchData.SortMode = SpriteSortMode.Deferred;
                spriteBatch.Begin(spriteBatchData);

                BetterLangMenuV2.HandleInteractions(in centered);
            }

            RecalculateButtonPosition(newRes);
            base.Draw(spriteBatch);

            UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;
        }
        void IHaveBackButtonCommand.HandleBackButtonUsage()
        {
            Main.MenuUI.SetState(null);
            Main.menuMode = MenuID.Settings;

            if (backButton != null)
            {
                backButton.grow = false;
                backButton.extraScale = 0f;
            }

            SoundEngine.PlaySound(in SoundID.MenuClose);
        }
    }
    public class BackButton : UIElement
    {
        private IHaveBackButtonCommand DoBackAction => Parent as IHaveBackButtonCommand;
        public bool grow = false;
        public float extraScale = 0f;
        public BackButton(float width, float height)
        {
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            OnMouseOver += Hovered;
            OnMouseOut += Unhovered;
            OnLeftClick += Clicked;
            OnUpdate += Upd;
        }

        private void Upd(UIElement affectedElement)
        {
            if (grow && extraScale < 1f)
                extraScale = Math.Min(extraScale + 0.1f, 1f);
            else if (!grow && extraScale > 0f)
                extraScale = Math.Max(extraScale - 0.1f, 0f);

            if (grow && !ContainsPoint(Main.MouseScreen))
                grow = false;
        }

        private void Unhovered(UIMouseEvent evt, UIElement listeningElement)
        {
            grow = false;
        }

        private void Hovered(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(in SoundID.MenuTick);
            grow = true;
        }

        private void Clicked(UIMouseEvent evt, UIElement listeningElement) => DoBackAction?.HandleBackButtonUsage();

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            string text = Lang.menu[5].Value;
            DynamicSpriteFont font = FontAssets.DeathText.Value;
            float finalScale = 0.75f + (extraScale * 0.3f);
            Vector2 center = GetDimensions().Center();
            Vector2 textSize = font.MeasureString(text) * finalScale;
            Color finalColor = MiscHelper.LerpMany(extraScale, [Color.Gray, Color.White, Color.Gold]);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, center - (textSize * 0.5f), finalColor, 0f, Vector2.Zero, new Vector2(finalScale));
        }
    }
}
