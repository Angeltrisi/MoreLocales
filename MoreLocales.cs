/*
 * Copyright (C) 2025 qAngel
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <https://www.gnu.org/licenses/>.
 */

global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Mono.Cecil.Cil;
global using MonoMod.Cil;
global using MoreLocales.Core;
global using MoreLocales.Utilities;
global using ReLogic.Graphics;
global using Terraria.ModLoader;
using MoreLocales.Common;
using System;
using System.Windows.Forms;
using Terraria.Localization;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
        public static MoreLocales Instance { get; private set; }
        static MoreLocales()
        {
            LocalizationTweaks.Apply();
        }
        public MoreLocales()
        {
            Instance = this;
            MoreLocalesAPI._canRegister = true;
            MoreLocalesAPI.DoLoad();
        }
        public override void Load()
        {
            AssetHelper.Setup(Instance);
            FontHelperV2.DoLoad();
            LangFeaturesPlus.DoLoad();
            MoreLocalesAPI.DoSafeLoad();
        }
        public override void PostSetupContent()
        {
            BetterLangMenuV2.InitAssetsSafe();

            MoreLocalesSets._contentReady = true;
            MoreLocalesSets.ReloadedLocalizations();

            MoreLocalesAPI.cachedVanillaCulture = LanguageManager.Instance.ActiveCulture.LegacyId;
            MoreLocalesAPI.LoadCustomCultureData();

            if (FontHelperV2.CharDataInlined && OperatingSystem.IsWindows())
                MessageBox.Show(GetLocalization("Misc.Error.FontPatchingError").Value, Language.GetTextValue("Error.Error"));
        }
        public override object Call(params object[] args)
        {
            throw new InvalidOperationException
                ("""
                MoreLocales does not have a Mod.Call API. Using a weakReference is your only option if you do not wish for this mod to be a dependency in your mod.
                This is in order to avoid extreme verbosity. Please consult the wiki for further information: https://github.com/queueAngel/MoreLocales/wiki/Home
                """);
        }
        public override void Unload()
        {
            MoreLocalesAPI.DoUnload();
            LocalizationTweaks.Unapply();
        }
    }
}
