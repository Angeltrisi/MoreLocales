using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Input;
using System;
using MoreLocales.Common;
using System.Reflection;
using Terraria.ID;
using System.Globalization;

namespace MoreLocales.Core
{
    public class MoreLocalesSystem : ModSystem
    {
        //private static bool testOverlap = false;
        public const int betterLangMenuID = 74592; //LANGS
        public static BetterLangMenuUI betterLangMenu = new();
        public override void Load()
        {
            IL_Main.DrawMenu += GoToBetterLangMenuInstead;
            //On_Main.DrawInterface += On_Main_DrawInterface;
        }
        // the docs for OnModLoad are wrong: it's called if all content is autoloaded specifically for the mod it's called on, not all mods.
        // so we use SetStaticDefaults instead
        public override void SetStaticDefaults()
        {
            MoreLocalesAPI._canRegister = false;
            // also, create the arrays for UI
            BetterLangMenuV2.InitArrays();
        }
        public override void OnLocalizationsLoaded()
        {
            MoreLocalesSets.ReloadedLocalizations();
        }
        private static void GoToBetterLangMenuInstead(ILContext il)
        {
            Mod mod = MoreLocales.Instance;
            try
            {
                var c = new ILCursor(il);

                if (!c.TryGotoNext(i => i.MatchLdcI4(1213), i => i.MatchStsfld<Main>("menuMode")))
                {
                    mod.Logger.Warn("GoToBetterLangMenuInstead: Couldn't find instruction for attempt to switch to lang menu");
                    return;
                }

                c.Next.Operand = betterLangMenuID;

                Type inter = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
                
                if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(inter.GetMethod("ModLoaderMenus", BindingFlags.NonPublic | BindingFlags.Static))))
                {
                    mod.Logger.Warn("GoToBetterLangMenuInstead: Couldn't find instruction for attempt to enter modded menus");
                    return;
                }

                c.EmitDelegate(TryEnterBetterLangMenu);

            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }
        private static void TryEnterBetterLangMenu()
        {
            if (Main.menuMode != betterLangMenuID)
                return;

            Main.MenuUI.SetState(betterLangMenu);
            Main.menuMode = MenuID.FancyUI;
        }
        #region DEBUGGING
        private static void On_Main_DrawInterface(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);

            string desiredFont = "MoreLocales/Assets/Fonts/MouseText-TH";
            if (!ModContent.HasAsset(desiredFont))
            {
                Main.NewText("Asset not found");
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Asset<DynamicSpriteFont> testFont = ModContent.Request<DynamicSpriteFont>(desiredFont, AssetRequestMode.ImmediateLoad);

            Vector2 padding = new(128f);
            float yBetween = 32f;
            float xBetween = 559f;

            SpriteBatch sb = Main.spriteBatch;
            DynamicSpriteFont testVanilla = FontAssets.CombatText[1].Value;

            for (int i = 0; i < 4; i++)
            {
                string testString = i switch
                {
                    0 => "abc01234",
                    1 => "áêç",
                    2 => "бгд",
                    3 => "เกี๊ยว",
                    _ => ""
                };

                for (int j = 0; j < 2; j++)
                {
                    DynamicSpriteFont font = j == 0 ? testVanilla : testFont.Value;
                    sb.DrawString(font, testString, padding + new Vector2(j == 0 ? 0 : false ? 0 : xBetween, i * yBetween), Color.White);
                }
            }

            Main.spriteBatch.End();
        }

        public override void PostUpdateDusts()
        {
            return;

            if (Main.keyState.IsKeyDown(Keys.F) && !Main.oldKeyState.IsKeyDown(Keys.F))
            {
                unsafe
                {
                    int nintSize = sizeof(nint);
                    int baseBytesCount = 0;
                    int thingsCount = 1; // dict object header
                    foreach (var thing in LangUtils._flattenedCache)
                    {
                        thingsCount++;

                        // GameCulture:
                        /// legacyID
                        thingsCount++;
                        baseBytesCount += sizeof(int);
                        // cultureinfo (really rough approximation)
                        thingsCount++;
                        CultureInfo info = thing.Key.CultureInfo;
                        /// isreadonly, isinherited
                        thingsCount += 2;
                        baseBytesCount += sizeof(bool) * 2;
                        /// name
                        thingsCount++;
                        baseBytesCount += info.Name.Length * sizeof(char);

                        // Dictionary<string, string>
                        for (int i = 0; i < thing.Value.Length; i++)
                        {
                            thingsCount++;
                            var dict = thing.Value[i];
                            foreach (var kvp in dict)
                            {
                                // each entry inside a dictionary is a Dictionary<TKey, TValue>.Entry
                                // then each entry holds both the key and value objects
                                thingsCount += 3;
                                baseBytesCount += (kvp.Key.Length + kvp.Value.Length) * sizeof(char);
                            }
                        }
                    }
                    Main.NewText(baseBytesCount + (thingsCount * nintSize));
                }
                //Main.NewText(LangUtils.Substitute("{$Title}", "Mods.MoreLocales.Cultures.MexicanSpanish.Title"));

                //MoreLocalesSets.ReloadedLocalizations();

                //var testDict = LangUtils.ParseVanillaLanguageFile(LangUtils.GetVanillaLanguageFilesForCulture(GameCulture.DefaultCulture)[6]);
                //if (testDict != null)
                {
                    //var newDict = LangUtils.FlattenVanillaLanguageDict(testDict);
                    //foreach (var kvp in newDict)
                    {
                        //Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                    /*
                    foreach (var kvp in testDict)
                    {
                        Console.WriteLine($"Original Key: {kvp.Key}");
                        Console.WriteLine("Subdict:");
                        foreach (var kvp2 in kvp.Value)
                        {
                            Console.WriteLine($"{kvp2.Key} :: {kvp2.Value}");
                        }
                        Console.WriteLine("End");
                    }
                    */
                }
                /*
                var files = LangUtils.GetLocalizationFiles(Mod, true);

                var firstFile = files[0];
                Console.WriteLine(firstFile.Name);
                LocalizationLoader.LocalizationFile localizationFile = firstFile.ToLocalizationFile(Mod);
                foreach (var entry in CollectionsMarshal.AsSpan(localizationFile.Entries))
                {
                    Console.Write(entry.comment);
                }
                */
                /*
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    Console.WriteLine(file.Name);

                }
                */
                /*
                string target = "pt-PT";
                if (LanguageManager.Instance.ActiveCulture.Name != target)
                    LanguageManager.Instance.SetLanguage(target);
                else
                    LanguageManager.Instance.SetLanguage("en-US");
                Main.NewText(LanguageManager.Instance.ActiveCulture.Name);
                */
            }
        }
        #endregion
    }
}
