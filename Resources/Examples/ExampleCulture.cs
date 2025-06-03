using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

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
        public override int CustomPluralizationRule(int count, int mod10, int mod100)
        {
            // This method should return the index of the pluralization type that we want to use.
            // An example of where this is used is in the mods creation menu, where you can see when you last built a certain mod.
            if (count <= 5)
                return 0;
            return Main.rand.Next(3);
        }
    }
}
