using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HackingGameUI
{
    public static class TerminalUIHelper
    {
        /// <summary>
        /// Translates a mouse position on a TextBox to a logical index used by the HackingTerminal BLL.
        /// </summary>
        public static int MapMouseToIndex(TextBox tb, Point position, int columnOffset)
        {
            // 1. Get the visual index (includes \r\n characters)
            int uiIndex = tb.GetCharacterIndexFromPoint(position, true);

            // 2. Boundary and Control character check
            if (uiIndex < 0 || char.IsControl(tb.Text[uiIndex]))
                return -1;

            // 3. Convert visual index to logical index by skipping control characters
            int localBllIndex = tb.Text.Substring(0, uiIndex).Count(c => !char.IsControl(c));

            return localBllIndex + columnOffset;
        }

        /// <summary>
        /// Finds the index in the visual string (with newlines) that corresponds 
        /// to a logical index (content only).
        /// </summary>
        public static int FindVisualIndex(string text, int targetLogicalIndex)
        {
            int logicalCounter = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (logicalCounter == targetLogicalIndex && !char.IsControl(text[i]))
                    return i;

                if (!char.IsControl(text[i]))
                    logicalCounter++;
            }
            return 0;
        }

        /// <summary>
        /// Calculates how many characters to select in the UI (including newlines) 
        /// to cover a specific length of logical content.
        /// </summary>
        public static int CalculateVisualLength(string text, int visualStart, int logicalLength)
        {
            int visualLength = 0;
            int logicalFound = 0;
            int currentIndex = visualStart;

            while (logicalFound < logicalLength && currentIndex < text.Length)
            {
                visualLength++;

                if (!char.IsControl(text[currentIndex]))
                {
                    logicalFound++;
                }

                currentIndex++;
            }
            return visualLength;
        }
    }
}
