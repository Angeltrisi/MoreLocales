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
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
            ExtraLocalesSupport.DoLoad();
        }
        public override void Load()
        {
            AssetHelper.Setup(Instance);
            FontHelperV2.DoLoad();
            FeaturesPlus.DoLoad();
            ExtraLocalesSupport.DoSafeLoad();
        }
        public override void PostSetupContent()
        {
            ExtraLocalesSupport.cachedVanillaCulture = LanguageManager.Instance.ActiveCulture.LegacyId;
            ExtraLocalesSupport.LoadCustomCultureData();

            if (FontHelperV2.CharDataInlined && OperatingSystem.IsWindows())
                MessageBox.Show(Language.GetTextValue("Mods.MoreLocales.Misc.Error.FontPatchingError"), Language.GetTextValue("Error.Error"));
        }
        public override object Call(params object[] args)
        {
            return base.Call(args);
        }
        public override void Unload()
        {
            ExtraLocalesSupport.DoUnload();
            LocalizationTweaks.Unapply();
        }
    }
}
