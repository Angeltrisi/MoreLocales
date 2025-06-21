using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace MoreLocales.Common
{
    /// <summary>
    /// <see href="https://bit.ly/3ZwJJaD"/>
    /// </summary>
    public static class BetterLangMenuV2
    {
        private readonly struct ButtonText
        {
            public readonly bool INIT;
            public readonly LocalizedText title;
            public readonly LocalizedText subtitle;
            public readonly LocalizedText description;
            public ButtonText(MoreLocalesCulture source)
            {
                string cultureName = source.Name;
                string culturesKey = $"Mods.{source.OwnerFunctionalName}.Cultures";
                string cultureKey = $"{culturesKey}.{cultureName}";

                title = Language.GetOrRegister($"{cultureKey}.Title");

                if (source.HasSubtitle)
                    subtitle = Language.GetOrRegister($"{cultureKey}.Subtitle");

                if (source.HasDescription)
                    description = Language.GetOrRegister($"{cultureKey}.Description");

                INIT = true;
            }
        }
        private struct ButtonDrawInfo
        {
            public bool currentDrawStepStatus;
            public Vector2 drawPositionCache;
            public Rectangle flagFrameCache;
            public int flagWidthCache;
            public ButtonText buttonText;
            public Color drawColor;
            public ButtonDrawState state;
            public bool hovered;
            public bool interactable;
            public DrawData? drawSelectionGraphic;
        }
        private enum ButtonDrawState
        {
            None,
            Selected,
            Unavailable
        }
        private static ButtonDrawInfo[] _drawInfoCache;
        internal static LangMenuV2 FinalRender => ModContent.GetInstance<LangMenuV2>();
        internal static Asset<Texture2D> _panelTexture;
        private static Asset<Texture2D> _panelHighlight;
        internal static Asset<Texture2D> _flagAtlas;
        private static Asset<Texture2D> _buttonPanel;
        internal const int PaddingX = 16;
        internal const int PaddingY = 16;
        internal const int PaddingXTotal = PaddingX * 2;
        internal const int PaddingYTotal = PaddingY * 2;
        internal static readonly Vector2 PaddingTotal = new(PaddingXTotal, PaddingYTotal);
        public const int FlagsCount = 28;
        internal static int columns = 2;
        internal static int rows = 5;
        internal static int currentPage = 0;
        internal static float currentPageVisual = 0f;
        internal static int buttonWidth = 1;
        internal static int buttonHeight = 1;
        internal static int buttonPaddingX = 16;
        internal static int buttonPaddingY = 0;
        // i was using the static constructor before but it stopped working for some reason lol
        internal static void InitAssetsSafe()
        {
            _panelTexture = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel");
            _panelHighlight = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel_Highlight");
            _flagAtlas = ModContent.Request<Texture2D>("MoreLocales/Assets/Flags");
            _buttonPanel = ModContent.Request<Texture2D>("MoreLocales/Assets/LangButton");
        }
        // runs when the custom cultures array has been finalized. called from MoreLocalesSystem
        internal static void InitArrays()
        {
            int length = MoreLocalesAPI.extraCulturesV2.Length;

            _drawInfoCache = new ButtonDrawInfo[length];
            for (int i = 1; i < _drawInfoCache.Length; i++)
            {
                ref var cache = ref _drawInfoCache[i];
                cache.buttonText = new ButtonText(MoreLocalesAPI.extraCulturesV2[i]);
                cache.drawColor = Color.White;
            }
        }
        private static List<int> indices;
        internal static void DrawTopLeft(SpriteBatch sb, int width, int height)
        {
            Texture2D buttonDrawTex = _buttonPanel.Value;

            buttonPaddingY = 0;

            if (buttonWidth == 1)
                buttonWidth = buttonDrawTex.Width;
            if (buttonHeight == 1)
                buttonHeight = (buttonDrawTex.Height / 2) - 2;

            // TODO: reduce amount of calculations by caching stuff

            //sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, width, height), Color.Red);

            MoreLocalesCulture[] cultures = MoreLocalesAPI.extraCulturesV2;

            currentPageVisual = MathHelper.Lerp(currentPageVisual, currentPage, 0.1f);

            float widthOfVisibleColumns = (buttonWidth + buttonPaddingX) * columns;
            float pageOffset = currentPageVisual * (widthOfVisibleColumns);
            float centerOffset = (width - widthOfVisibleColumns + buttonPaddingX) * 0.5f;

            float xOffset = ((int)((-pageOffset + centerOffset) * 0.5f) * 2); // -pageOffset + centerOffset

            float heightOfVisibleRows = (buttonHeight + buttonPaddingY) * rows;
            // we actually need to remove this amount from heightOfVisibleRows due to the overlap lol

            float yOffset = ((int)(((height - heightOfVisibleRows + buttonPaddingY) * 0.5f) * 0.5f) * 2); // (height - heightOfVisibleRows + buttonPaddingY) * 0.5f;

            int itemsPerPage = Math.Max(columns * rows, 1);
            int maxPage = ((cultures.Length - 1) + itemsPerPage - 1) / itemsPerPage;

            if (PageChanged(out int direction))
            {
                currentPage += direction;

                float overshoot = 0.05f;

                if (currentPage < 0)
                {
                    currentPage = 0;
                    currentPageVisual -= overshoot;
                }
                else if (currentPage >= maxPage)
                {
                    currentPage = maxPage - 1;
                    currentPageVisual += overshoot;
                }
            }

            if (currentPage >= maxPage)
                currentPage = maxPage - 1;

            // i'm not entirely clear on what the better way to do this is
            // caches visible buttons for later drawing
            indices ??= new(cultures.Length - 1);
            for (int i = 1; i < cultures.Length; i++)
            {
                ref var culture = ref cultures[i];
                if (culture.Available is null || culture.Available())
                    indices.Add(i);
            }

            var span = CollectionsMarshal.AsSpan(indices);

            // passes start. we can use this pass to both set up the draw position cache, and to draw the panel
            // this pass needs to be done with a for loop, as 'i' gives us the info we need to get the final position of the button

            Rectangle frame = new(0, 0, buttonWidth, buttonHeight + 2);

            for (int i = 0; i < span.Length; i++)
            {
                // layout should be like this if 2 is columns amt and 5 is rows amt for example:

                // first    second  eleventh    twelfth
                // third    fourth  thirteenth  fourteenth
                // fifth    sixth   fifteenth   sixteenth
                // seventh  eighth  seventeenth eighteenth
                // ninth    tenth   nineteenth  twentieth

                int column = i % (columns) + (i / itemsPerPage * columns); // the design is very human
                int row = (i / columns) % rows;

                int index = span[i]; // index of the morelocalesculture in morelocalesAPI.extraculturesv2

                Vector2 topLeftNoOffset = new((column * (buttonWidth + buttonPaddingX)), (row * (buttonHeight + buttonPaddingY)));
                Vector2 topLeftDraw = new(topLeftNoOffset.X + xOffset, topLeftNoOffset.Y + yOffset);

                ref ButtonDrawInfo info = ref _drawInfoCache[index];
                info.drawPositionCache = topLeftDraw;

                bool interact = info.interactable = !ButtonActive(index);

                ref MoreLocalesCulture culture = ref cultures[index];

                Color drawColor;

                if (interact)
                {
                    drawColor = Color.White;
                    if (info.hovered)
                        drawColor.A = 80; // i was just testing but this looks kinda good lol
                }
                else
                    drawColor = Color.Gray;

                drawColor = info.drawColor = Color.Lerp(info.drawColor, drawColor, 0.2f);

                Vector2 origin = frame.Size() * 0.5f;

                DrawData buttonDrawData = new(buttonDrawTex, topLeftDraw + origin, frame, drawColor, 0f, origin, 1f, SpriteEffects.None);
                ButtonPanelDraw hijackPanelDraw = culture.ButtonDrawData.HijackPanelDraw;

                bool? decision = hijackPanelDraw == null ? true : hijackPanelDraw(ref buttonDrawData);

                // for panels that use shaders, we need to make sure spritebatch is flushed so only this specific button is affected
                // otherwise buttons drawn before will be affected as their vertices will be flushed on .End() too.

                // buttonDrawData.shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingRainbowDye);

                bool willDraw = decision.HasValue && decision.Value;

                if (willDraw)
                {
                    if (buttonDrawData.shader > 0)
                    {
                        sb.End(out var spriteBatchData);
                        sb.Begin(spriteBatchData with { SortMode = SpriteSortMode.Immediate });

                        var shader = GameShaders.Armor._shaderData[buttonDrawData.shader - 1];
                        shader.Apply(null, buttonDrawData);
                    }

                    buttonDrawData.Draw(sb);

                    if (buttonDrawData.shader > 0)
                    {
                        sb.End(out var spriteBatchData);
                        sb.Begin(spriteBatchData with { SortMode = SpriteSortMode.Deferred });
                    }

                    if (interact && info.hovered)
                    {
                        Rectangle frame2 = frame;
                        frame2.Y += buttonHeight + 2;
                        Color color = Main.OurFavoriteColor;

                        info.drawSelectionGraphic = buttonDrawData with { sourceRect = frame2, color = color };

                        //sb.Draw(buttonDrawTex, buttonDrawData.position, frame2, color, buttonDrawData.rotation, buttonDrawData.origin, buttonDrawData.scale, buttonDrawData.effect, 0f);
                    }
                    else
                    {
                        info.drawSelectionGraphic = null;
                    }
                }

                info.currentDrawStepStatus = decision ?? true;
            }

            // second pass: drawing the flag icon and the selection graphic

            Vector2 flagOffset = new(6f);

            foreach (var index in span)
            {
                ref ButtonDrawInfo info = ref _drawInfoCache[index];

                if (info.currentDrawStepStatus)
                {
                    ref MoreLocalesCulture culture = ref cultures[index];

                    Vector2 topLeftDraw = info.drawPositionCache;

                    ref Rectangle flagFrame = ref info.flagFrameCache;

                    ref Asset<Texture2D> possibleSheet = ref culture.ButtonDrawData.Sheet;
                    possibleSheet ??= _flagAtlas;

                    Texture2D sourceTexture = possibleSheet.Value;

                    ref int flagWidth = ref info.flagWidthCache; // used for text pass
                    if (flagWidth == 0)
                        flagWidth = sourceTexture.Width;

                    if (flagFrame.IsEmpty)
                        flagFrame = sourceTexture.Frame(1, culture.ButtonDrawData.SheetFrameCount, 0, culture.ButtonDrawData.SheetFrame);

                    Vector2 origin = flagFrame.Size() * 0.5f;
                    Vector2 flagDrawPos = topLeftDraw + flagOffset + origin;
                    sb.Draw(sourceTexture, flagDrawPos, flagFrame, info.drawColor, 0f, origin, 1f, SpriteEffects.None, 0f);
                }

                info.drawSelectionGraphic?.Draw(sb);
            }

            // final pass: drawing the text

            foreach (var index in span)
            {
                ref ButtonDrawInfo info = ref _drawInfoCache[index];

                if (!info.currentDrawStepStatus) // skip if the status for this button is false
                    continue;

                ref MoreLocalesCulture culture = ref cultures[index];

                Vector2 topLeftDraw = info.drawPositionCache;

                DynamicSpriteFont font = FontAssets.MouseText.Value;
                ref ButtonText text = ref info.buttonText;
                Color drawColor = info.drawColor;

                // draw language code text

                string cultureName = $"({culture.Culture.Name})";
                float flagCenterX = topLeftDraw.X + flagOffset.X + (info.flagWidthCache * 0.5f);
                float cultureScale = 0.75f;
                float cultureSizeX = font.MeasureString(cultureName).X * cultureScale;
                float yOff = 40f;
                Vector2 drawCodePos = new Vector2(flagCenterX - cultureSizeX * 0.5f, topLeftDraw.Y + yOff).Floor();
                ChatManager.DrawColorCodedStringWithShadow(sb, font, cultureName, drawCodePos, drawColor, 0f, Vector2.Zero, new Vector2(cultureScale));

                // draw title and subtitle

                Vector2 centerForTitles = topLeftDraw + new Vector2(buttonWidth + info.flagWidthCache + flagOffset.X, buttonHeight) * 0.5f;

                bool sub = culture.HasSubtitle;

                if (sub)
                {
                    float subSize = 0.85f;
                    string subtitle = text.subtitle.Value;
                    float xSize = font.MeasureString(subtitle).X * subSize;
                    Color drawSubColor = Color.Lerp(drawColor, Color.Black, 0.25f);
                    Vector2 drawSubPos = (centerForTitles - new Vector2(xSize * 0.5f, 0f)).Floor();
                    ChatManager.DrawColorCodedStringWithShadow(sb, font, subtitle, drawSubPos, drawSubColor, 0f, Vector2.Zero, new Vector2(subSize));
                }

                string title = info.buttonText.title.Value;
                float xSizeTitle = font.MeasureString(title).X;
                Vector2 drawTitlePos = (centerForTitles - new Vector2(xSizeTitle * 0.5f, sub ? 18f : 10f)).Floor();
                ChatManager.DrawColorCodedStringWithShadow(sb, font, title, drawTitlePos, drawColor, 0f, Vector2.Zero, Vector2.One);
            }

            // we can reset the list when handling interactions
        }
        internal static void HandleInteractions(in Rectangle containerScreen)
        {
            if (indices is null)
                return;

            int mouseX = Main.mouseX;
            int mouseY = Main.mouseY;

            Span<int> span = CollectionsMarshal.AsSpan(indices);

            if (!containerScreen.Contains(mouseX, mouseY))
            {
                foreach (var index in span)
                    _drawInfoCache[index].hovered = false;

                indices.Clear();
                return;
            }

            foreach (var index in span)
            {
                ref ButtonDrawInfo info = ref _drawInfoCache[index];

                Vector2 positionScreen = info.drawPositionCache + new Vector2(containerScreen.X, containerScreen.Y);

                Rectangle buttonRect = new((int)positionScreen.X, (int)positionScreen.Y, buttonWidth, buttonHeight);

                if (!buttonRect.Contains(mouseX, mouseY))
                {
                    info.hovered = false;
                    continue;
                }

                // handle description

                LocalizedText possibleDesc = info.buttonText.description;
                if (possibleDesc != null)
                {
                    Main.instance.MouseText(possibleDesc.Value);
                }

                // handle hovering

                bool interact = info.interactable;

                if (interact && !info.hovered)
                    SoundEngine.PlaySound(in SoundID.MenuTick);
                info.hovered = true;

                ref MoreLocalesCulture culture = ref MoreLocalesAPI.extraCulturesV2[index];

                // handle clicking

                if (interact && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    LanguageManager.Instance.SetLanguage(culture.Culture);
                    SoundEngine.PlaySound(in SoundID.MenuOpen);
                }

                // we could break here but then hovering wouldn't be handled properly and that's not goods
                // break;
            }

            indices.Clear();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ButtonActive(int index)
        {
            return MoreLocalesAPI.extraCulturesV2[index].Culture.LegacyId == LanguageManager.Instance.ActiveCulture.LegacyId;
        }
        private static bool PageChanged(out int direction)
        {
            static bool JustPressed(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);

            direction = 0;

            bool goRight = JustPressed(Keys.E);
            bool goLeft = JustPressed(Keys.Q);

            if (goRight || goLeft)
            {
                if (goRight)
                    direction++;
                if (goLeft)
                    direction--;
                return true;
            }
            return false;
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
            // my initial approach was just dividing screenwidth by some number and similarly subtracting  some number from screenheight
            // however, that doesn't feel clean: there's a very large padding difference between the UI frame and the buttons
            // so instead, we can calculate the amount of buttons we can fit here, and base the final size on that.

            MoreLocalesCulture[] cultures = MoreLocalesAPI.extraCulturesV2;

            int availableHorizontal = (int)(Main.screenWidth * 0.5f);
            int availableVertical = Main.screenHeight - 430;

            BetterLangMenuV2.columns = Math.Max(availableHorizontal / (BetterLangMenuV2.buttonWidth + BetterLangMenuV2.buttonPaddingX), 1);
            BetterLangMenuV2.rows = Math.Max(Math.Min((int)(cultures.Length / (float)BetterLangMenuV2.columns), availableVertical / (BetterLangMenuV2.buttonHeight + BetterLangMenuV2.buttonPaddingY)), 1);

            int sizeX = BetterLangMenuV2.columns * (BetterLangMenuV2.buttonWidth + BetterLangMenuV2.buttonPaddingX);
            int sizeY = BetterLangMenuV2.rows * (BetterLangMenuV2.buttonHeight + BetterLangMenuV2.buttonPaddingY) + BetterLangMenuV2.buttonPaddingX;

            base.PrepareARenderTarget_AndListenToEvents(ref _target, device, sizeX, sizeY, RenderTargetUsage.PlatformContents);

            var oldTargets = device.GetRenderTargets();

            device.SetRenderTarget(_target);
            device.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
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
}
