using System.IO;

namespace MoreLocales.Utilities
{
    /*
     * MIT License
     *
     * Copyright (c) 2025 John Baglio
     *
     * https://github.com/absoluteAquarian/SerousCommonLib/blob/master/src/API/Helpers/ILHelper.cs
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     * copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in all
     * copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
     * SOFTWARE.
     */
    internal static class ILHelper
    {
        /// <summary>
		/// Updates the instruction offsets within <paramref name="c"/>
		/// </summary>
		/// <param name="c">The cursor</param>
		public static void UpdateInstructionOffsets(ILCursor c)
        {
            var instrs = c.Instrs;
            int curOffset = 0;

            static Instruction[] ConvertToInstructions(ILLabel[] labels)
            {
                Instruction[] ret = new Instruction[labels.Length];

                for (int i = 0; i < labels.Length; i++)
                    ret[i] = labels[i].Target;

                return ret;
            }

            foreach (var ins in instrs)
            {
                ins.Offset = curOffset;

                if (ins.OpCode != OpCodes.Switch)
                    curOffset += ins.GetSize();
                else
                {
                    //'switch' opcodes don't like having the operand as an ILLabel[] when calling GetSize()
                    //thus, this is required to even let the mod compile

                    Instruction copy = Instruction.Create(ins.OpCode, ConvertToInstructions((ILLabel[])ins.Operand));
                    curOffset += copy.GetSize();
                }
            }
        }
        public static void DumpIL(ILContext il, string folderName)
        {
            string methodName = il.Method.FullName.Replace(':', '_').Replace('<', '[').Replace('>', ']');
            if (methodName.Contains('?')) // MonoMod IL copies are created with mangled names like DMD<Terraria.Player::beeType>?38504011::Terraria.Player::beeType(Terraria.Player)
                methodName = methodName[(methodName.LastIndexOf('?') + 1)..];
            methodName = string.Join("_", methodName.Split(Path.GetInvalidFileNameChars())); // Catch any other illegal characters, just in case.

            string filePath = Path.Combine(Logging.LogDir, "ILDumps", folderName, methodName + ".txt");
            string folderPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            File.WriteAllText(filePath, il.ToString());

            Logging.tML.Debug($"Dumped ILContext \"{il.Method.FullName}\" to \"{filePath}\"");
        }
    }
}
