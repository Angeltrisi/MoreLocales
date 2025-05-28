using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MoreLocales.Config
{
    public class ClientSideConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
#pragma warning disable
        public static ClientSideConfig Instance;
#pragma warning restore 
        [Header("$Mods.MoreLocales.Configs.Headers.Features")]
        [DefaultValue(false)]
        public bool LocalizedPrefixPlacement;

        [DefaultValue(true)]
        public bool LocalizedPrefixGenderPluralization;

        [Header("$Mods.MoreLocales.Configs.Headers.Fonts")]
        [DefaultValue(LocalizedFont.None)]
        [DrawTicks]
        public LocalizedFont ForcedFont;
    }
}
