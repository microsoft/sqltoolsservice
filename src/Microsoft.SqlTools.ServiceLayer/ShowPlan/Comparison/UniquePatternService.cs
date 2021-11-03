//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Drawing;
using System.Linq;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Comparison
{
    internal class UniquePatternService
    {
        /// <summary>
        /// Colors taken from Ibiza color palette
        /// Labels used for easy identification in case some need to be removed or more need to be added
        /// https://df.onecloud.azure-test.net/?SamplesExtension=true#blade/SamplesExtension/StyleGuideColorPaletteBlade
        /// </summary>
        private static Color[] Colors = new []
        {
            Color.FromArgb(0, 188, 242),    // "themeMain blue"
            Color.FromArgb(236, 0, 140),    // "themeError pink"
            Color.FromArgb(0, 216, 204),    // "h2 blue"
            Color.FromArgb(236, 0, 140),    // "b0 orange"
            Color.FromArgb(255, 140, 0),    // "themeWarning orange"
            Color.FromArgb(127, 186, 0),    // "themeSuccess green"
            Color.FromArgb(252, 214, 241),  // "paletteDiffDel light pink"
            Color.FromArgb(252, 209, 22),   // "a1 gold"
            Color.FromArgb(68,35,89),       // "e1 dark purple"
            Color.FromArgb(0, 114, 198),    // "g1 blue"
            Color.FromArgb(160, 165, 168),  // "i1 green"
            Color.FromArgb(255, 140, 0),    // "k1 grey"
            Color.FromArgb(199, 241, 199),  // "paletteDiffAdd light green"
            Color.FromArgb(0, 24, 143),     // "d0 pink",
            Color.FromArgb(186, 216, 10),   // "f0 royal blue"
            Color.FromArgb(255, 252, 158),  // "h0 seafoam green"
            Color.FromArgb(221, 89, 0),     // "j0 yellow green"
            Color.FromArgb(155, 79, 150),   // "a2 light yellow"
            Color.FromArgb(109, 194, 233),  // "c2 burnt orange"
            Color.FromArgb(85, 212, 85),    // "e2 purple"
            Color.FromArgb(180, 0, 158),    // "d1 purple"
            Color.FromArgb(0, 32, 80),      // "f1 navy blue"
            Color.FromArgb(0, 130, 114),    // "h1 blue green"
            Color.FromArgb(127, 186, 0),    // "j1 yellow green"
            Color.FromArgb(255, 241, 0),    // "a0 bright yellow"
            Color.FromArgb(104, 33, 122),   // "e0 purple"
            Color.FromArgb(0, 188, 242),    // "g0 sky blue"
            Color.FromArgb(0, 158, 73),     // "i0 green"
            Color.FromArgb(187, 194, 202),  // "k0 grey"
            Color.FromArgb(255, 185, 0),    // "b2 gold"
            Color.FromArgb(244, 114, 208),  // "d2 pink"
            Color.FromArgb(70, 104, 197),   // "f2 blue purple"
            Color.FromArgb(226, 229, 132),  // "j2 khaki"
        };

        private static float[][] DashPatterns = new float [][]
        {
            null,
            new float[] { 1.0f, 1.0f},
            new float[] { 2.0f, 2.0f, 2.0f},
        };

        public static float[] GetDashPattern(int groupIndex)
        {
            return DashPatterns[groupIndex % DashPatterns.Count()]; 
        }

        public static Color GetColor(int groupIndex)
        {
            return Colors[groupIndex % Colors.Count()];
        }

        public static Color PaleYellow = Color.FromArgb(248, 244, 153);
    }
}
