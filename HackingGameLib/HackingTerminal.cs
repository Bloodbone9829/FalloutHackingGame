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
        public string OpeningBrackets = "({[<";

        private Dictionary<int, string> _wordLocationsDict = new Dictionary<int, string>();
        public void StartGame(int difficulty)
        {
            // 1. Get words from your WordBank (assuming you implemented it)
            // var words = WordBank.GetWords(length: 5 + difficulty, count: 15);

            // 2. Pick Password
            // Password = words[new Random().Next(words.Count)];

            // 3. GENERATE BOARD (Simplified Token approach)
            // Create a 408-char string of junk symbols.
            // Insert the words at random random intervals.
            // Insert specific matching brackets (e.g. '[' and ']') exactly 6 chars apart.

            // BoardState = ... (result of generation);

            // 1. Reset the dictionary for the new game
            _wordLocationsDict.Clear();

            // ... (Your logic to get words from WordBank) ...
            List<string> wordsToPlace = WordBank.GetWords(5 + difficulty, 15);
            CorrectPassword = wordsToPlace[new Random().Next(wordsToPlace.Count)];

            // 2. Setup the empty board (e.g. 408 random characters)
            char[] boardChars = GenerateRandomGarbage(408);

            // 3. Place the words and record their locations
            Random rnd = new Random();

            foreach (string word in wordsToPlace)
            {
                int position;
                bool placed = false;

                // Try to find a spot that doesn't overlap other words
                while (!placed)
                {
                    position = rnd.Next(0, boardChars.Length - word.Length);

                    // (You would add logic here to ensure it doesn't overlap existing words)
                    // If safe:
                    {
                        // Write word to board array
                        for (int i = 0; i < word.Length; i++)
                            boardChars[position + i] = word[i];

                        // *** THIS IS THE KEY PART ***
                        _wordLocationsDict.Add(position, word);

                        placed = true;
                    }
                }
            }

            // 4. Finalize Board
            BoardState = new string(boardChars);
        }
        private char[] GenerateRandomGarbage(int length)
        {
            const string garbageChars = "!@#$%^&*()_+-=[]{}|;:,.<>/?`~";
            Random rnd = new Random();
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = garbageChars[rnd.Next(garbageChars.Length)];
            }
            return result;
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

        private bool IsStartBracket(char c) => "({[<".Contains(c);

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
