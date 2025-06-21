using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using MoreLocales.Common;
using MoreLocales.Core;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace MoreLocales.Resources.Examples
{
    // Since this class implements ILoadable, we can use AutoloadAttribute to stop loading conditionally.
    [Autoload(false)]
    public class ExampleCulture : ModCulture
    {
        // Overriding this getter is mandatory, since this is the identifier used for loading localization files.
        public override string LanguageCode => "ex-EX";
        // This is the identifier used for stuff like creating keys for the display name of this culture.
        // If we leave it like this, keys for this culture will be registered under 'ExampleCulture', which is the class name.
        public override string Name => base.Name;
        // ModCulture has a couple of useful Set methods for setting a range of different aspects of this culture concisely.
        public override void SetCultureData(ref int fallbackCulture, ref bool hasSubtitle, ref bool hasDescription)
        {
            // I'm feeling British today. If the game doesn't find localized values for something, we can make it fall back to British English, for example.
            fallbackCulture = (int)CultureNamePlus.BritishEnglish;
            // I think we can have a subtitle, so we'll leave hasSubtitle like it is right now (true),
            // but I also want some text to show up when the button is hovered over, so we can change hasDescription to be true.
            hasDescription = true;
        }
        public override void SetGrammarData(ref PluralizationStyle pluralizationStyle, ref AdjectiveOrder adjectiveOrder)
        {
            // Hm, none of the regular PluralizationStyle types really catch my attention, so I guess we can make it custom.
            // Since I want custom a custom pluralization rule, we're gonna have to override the CustomPluralizationRule method as well.
            pluralizationStyle = PluralizationStyle.Custom;

            // AdjectiveOrder has a couple of presets, but again, none of them are really what I'm looking for.
            // We can make our own using the constructor.

            // (I want my adjectives to be formatted in a sort of fun way, maybe like this: Noun★Adjective)
            adjectiveOrder = new AdjectiveOrder(AdjectiveOrderType.After, "★");
        }
        // We gotta do this, since we set this culture's pluralization style to Custom.
        public override int CustomPluralizationRule(int count, int mod10, int mod100)
        {
            // This method should return the index of the pluralization type that we want to use.
            // An example of where this is used is in the mods creation menu, where you can see when you last built a certain mod.
            if (count <= 5)
                return 0;
            return Main.rand.Next(3);
        }
        // Not really necessary, but a little performance improvement is always good.
        public override bool ContextChangesAdjective(GrammaticalGender gender, Pluralization pluralization)
        {
            return gender == GrammaticalGender.Feminine;
        }
        // We need to make sure the button looks good.
        public override void SetButtonDrawData(ref Asset<Texture2D> sheet, ref int? sheetFrameCount, ref int? sheetFrame)
        {
            // I'm still feeling quite British, so I think it's fine if we leave the sheet null (it will default to MoreLocales' Flags.png)
            // As for the frame count, it'll default to the flags amount, because we left the sheet null. So we also don't mess with that.
            // Flags.png is arranged to match with culture legacy IDs, so we can get the UK flag like this:
            sheetFrame = (int)CultureNamePlus.BritishEnglish;

            /*
            
            // If we do wanna supply our own custom sprite, that's also easy:
            sheet = Mod.Assets.Request<Texture2D>("Assets/Flags");
            // But then we also need to supply the frame count.
            sheetFrameCount = BetterLangMenuV2.FlagsCount;

            */
        }
        // That may be enough, but I kinda want more.
        public override bool? PreDrawButtonPanel(ref DrawData drawData)
        {
            // Panels can be dyed.
            drawData.shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingFlameDye);
            // And jostled around all we want.
            float amt = 16f;
            Vector2 offset = new((float)Math.Cos(Main.timeForVisualEffects * 2f) * amt, (float)Math.Sin(Main.timeForVisualEffects) * amt); // infinite symbol :P
            drawData.position += offset;
            drawData.rotation = (float)Math.Sin(Main.timeForVisualEffects * 2f) * 0.5f;
            return true;
        }
    }
}
