using System;
using System.Collections.Generic;
using System.Text;

namespace HackingGameLib
{

    internal class GameConfiguration
    {
        internal int StartingAttempts { get; set; } = 4;
        internal int WordCount { get; set; } = 15;
        internal int BaseWordLength { get; set; } = 5;
        internal int ColumnCount { get; set; } = 12;
        internal int MinRowsCount { get; set; } = 32;
        internal int BracketSearchDistance { get; set; } = 10;

        internal readonly Dictionary<char, char> BracketPairs = new Dictionary<char, char>
        {
            { '(', ')' },
            { '{', '}' },
            { '[', ']' },
            { '<', '>' }
        };


        // Pro Tip: The "Factory" method that returns a preset
        internal static GameConfiguration GetPreset(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => new GameConfiguration
                {
                    StartingAttempts = 5,
                    WordCount = 10,
                    BaseWordLength = 4
                },
                DifficultyLevel.Hard => new GameConfiguration
                {
                    StartingAttempts = 5,
                    WordCount = 15,
                    BaseWordLength = 5
                },
                DifficultyLevel.Advanced => new GameConfiguration
                {
                    StartingAttempts = 4,
                    WordCount = 15,
                    BaseWordLength = 6
                },

                DifficultyLevel.Hardcore => new GameConfiguration
                {
                    StartingAttempts = 3,
                    WordCount = 20,
                    BaseWordLength = 8,
                    BracketSearchDistance = 5 // Harder because you can't "find" as many duds
                },
                _ => new GameConfiguration() // Default
            };
        }
    }
}