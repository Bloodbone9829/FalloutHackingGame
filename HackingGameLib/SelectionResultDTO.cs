using System;
using System.Collections.Generic;
using System.Text;

namespace HackingGameLib
{
    public enum GameStatus { Playing, Won, Lost }

    public class SelectionResultDTO
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string SelectedText { get; set; }
        public bool IsValidSelection { get; set; } // True if it is a word or a bracket pair
        public bool IsWord { get; set; } // True if it is a valid guess of a word
        public GameStatus Status { get; set; } = GameStatus.Playing;
    }
}
