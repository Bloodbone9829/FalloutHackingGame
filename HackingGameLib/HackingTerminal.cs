using System;
using System.Collections.Generic;
using System.Text;
namespace HackingGameLib
{
    public class HackingTerminal
    {
        public string BoardState { get; private set; } // A massive string of symbols, letters, and brackets
        public List<string> ActiveWords { get; private set; } // Words that are currently "active" on the board (not yet guessed)
        public string CorrectPassword { get; private set; } // The correct password the player is trying to guess
        public int RemainingAttempts { get; private set; } = 4; // Number of attempts left before lockout

        public event Action<string> OnGameMessage; 
        public event Action<int> OnAttemptsUpdate; // Updates the UI. Pass the new attempts count.
        private const string OpeningBrackets = "({[<";
        private const string GarbageChars = "!@#$%^&*()_+-=[]{}|;:,.<>/?`~";
        private Random _random;
        private const int ColumnCount = 12; // number of characters per line on the board (for formatting purposes)
        private const int MinRowsCount = 32; // Standard Fallout screen height
        private Dictionary<int, string> _wordLocationsDict = new Dictionary<int, string>();

        private int _currentActiveRows; // Tracks how many rows are currently active based on the number of words and board size. This can be used for UI scaling

        public HackingTerminal()
        {
            _random = new Random();
        }
        public void StartGame(int difficulty = 0)
        {
            _wordLocationsDict.Clear();

            // 1. Configuration
            var wordsToPlace = GetWordsForDifficulty(difficulty);
            CorrectPassword = SelectRandomPassword(wordsToPlace);

            // 2. Initialization
            _currentActiveRows = CalculateRequiredRows(wordsToPlace.Count, wordsToPlace[0].Length);
            int totalBoardSize = _currentActiveRows * ColumnCount;

            char[] boardBuffer = GenerateRandomGarbage(totalBoardSize);
            // 3. Core Logic
            PlaceWordsOnBoard(boardBuffer, wordsToPlace);

            // 4. Finalization
            BoardState = new string(boardBuffer);
        }

        // Checks if the specified range on the board is free of letters (i.e. safe for placing a new word)
        private bool IsSpaceAvailable(char[] board, int position, int length)
        {
            // Your LINQ logic lives here now. 
            // It is isolated, easy to read, and easy to change later.
            return !board
                    .Skip(position)
                    .Take(length)
                    .Any(char.IsLetter); // Note: You can use Method Group syntax here!
        }

        private void PlaceWordsOnBoard(char[] board, List<string> words)
        {
            foreach (string word in words)
            {
                if (!TryPlaceSingleWord(board, word))
                {
                    // Socratic Question: What should happen if a word simply 
                    // CANNOT fit after 100 tries? Should the game crash, 
                    // or should we just skip that word?
                }
            }
        }

        // Tries to place a single word on the board. Returns true if successful, false if it fails after max attempts.
        private bool TryPlaceSingleWord(char[] board, string word)
        {
            int maxAttempts = 100;

            for (int attempts = 0; attempts < maxAttempts; attempts++)
            {
                int position = _random.Next(0, board.Length - word.Length);

                if (IsSpaceAvailable(board, position, word.Length))
                {
                    WriteWordToBuffer(board, word, position);
                    _wordLocationsDict.Add(position, word); // Side effect: Updating state
                    return true; // Success!
                }
            }
            return false; // Failed to place
        }

        // Writes the word into the board array at the specified position
        private void WriteWordToBuffer(char[] board, string word, int startPosition)
        {
            for (int i = 0; i < word.Length; i++)
            {
                board[startPosition + i] = word[i];
            }
        }

        // generates random garbage characters to fill the board, ensuring we have a mix of symbols and brackets for the player to interact with.
        private char[] GenerateRandomGarbage(int length)
        {
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = GarbageChars[_random.Next(GarbageChars.Length)];
            }
            return result;
        }

     
        private List<string> GetWordsForDifficulty(int difficulty)
        {
            // The logic: 5 chars is base, difficulty adds to length. 
            // We always want 15 words.
            int wordLength = 5 + difficulty;
            int wordCount = 15;

            return WordBank.GetWords(wordLength, wordCount);
        }

        private string SelectRandomPassword(List<string> words)
        {
            // Safety check: ensures the list isn't empty
            if (words == null || words.Count == 0)
                throw new InvalidOperationException("Word list cannot be empty.");

            int index = _random.Next(words.Count);
            return words[index];
        }

        // This method calculates how many rows we need to generate based on the number of words and their length, ensuring we have enough garbage to fill the board and create a good experience.
        private int CalculateRequiredRows(int wordCount, int wordLength)
        {
            int rawContentSize = wordCount * wordLength;
            int minSafeSize = rawContentSize * 2;

            int rowsRequired = (int)Math.Ceiling((double)minSafeSize / ColumnCount); 

            return Math.Max(MinRowsCount, rowsRequired);
        }

        public int CheckLikeness(string guess)
        {
            // Lambda: Count chars that match both value and index
            return guess.Where((c, i) => i < CorrectPassword.Length && c == CorrectPassword[i]).Count();
        }

        public SelectionResultDTO GetSelection(int index)
        {
            // 1. BOUNDARY CHECK
            if (index < 0 || index >= BoardState.Length)
                return new SelectionResultDTO();

            // 2. CHECK IF IT IS A WORD (Prioritize words over brackets)
            var wordResult = CheckIfWord(index);
            if (wordResult != null) return wordResult;

            // 3. CHECK IF IT IS A BRACKET PAIR
            char startChar = BoardState[index];

            // Only proceed if we clicked an OPENING bracket
            if (IsStartBracket(startChar))
            {
                char expectedCloser = GetMatchingCloser(startChar);
                int maxDist = 10; // The "set amount" you requested

                // LOOK AHEAD LOOP
                for (int i = 1; i <= maxDist; i++)
                {
                    int checkIndex = index + i;

                    // Stop if we run off the end of the string
                    if (checkIndex >= BoardState.Length) break;

                    char checkChar = BoardState[checkIndex];

                    // A. FOUND THE MATCH!
                    if (checkChar == expectedCloser)
                    {
                        return new SelectionResultDTO
                        {
                            StartIndex = index,
                            Length = i + 1, // Distance + 1 for the closer itself
                            SelectedText = BoardState.Substring(index, i + 1),
                            IsValidSelection = true,
                            IsWord = false
                        };
                    }

                    // B. HIT A WORD? (Optional Fallout Rule)
                    // In the real game, a bracket pair cannot "eat" a word.
                    // If we hit a letter, this bracket is a "dud" (broken pair).
                    if (char.IsLetterOrDigit(checkChar))
                    {
                        break;
                    }
                }
            }

            // 4. DEFAULT: Just return the single character (Invalid selection)
            return new SelectionResultDTO
            {
                StartIndex = index,
                Length = 1,
                SelectedText = startChar.ToString(),
                IsValidSelection = false
            };
        }

        public TerminalSettingsDTO GetTerminalSettings()
        {
            // Safety: If game hasn't started, return 0 or calculate it manually
            int actualLength = BoardState != null ? BoardState.Length : 0;
            return new TerminalSettingsDTO
            {
                Columns = ColumnCount,
                Rows = _currentActiveRows,
                TotalLength = actualLength
            };
        }
        private SelectionResultDTO CheckIfWord(int index)
        {
            // Loop through all known word positions
            foreach (var entry in _wordLocationsDict)
            {
                int wordStartIndex = entry.Key;
                string word = entry.Value;
                int wordEndIndex = wordStartIndex + word.Length;

                // Check if the clicked index is inside this word
                if (index >= wordStartIndex && index < wordEndIndex)
                {
                    return new SelectionResultDTO
                    {
                        StartIndex = wordStartIndex,
                        Length = word.Length,
                        SelectedText = word,
                        IsValidSelection = true,
                        IsWord = true // Important flag!
                    };
                }
            }

            // If no word was found at this index
            return null;
        }

        private bool IsStartBracket(char c) => OpeningBrackets.Contains(c);

        private char GetMatchingCloser(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case '{': return '}';
                case '[': return ']';
                case '<': return '>';
                default: return '\0'; // Return null char if no match
            }
        }
    }
}
