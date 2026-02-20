using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;
namespace HackingGameLib
{
    public class HackingTerminal
    {
        public string BoardState { get; private set; } // A massive string of symbols, letters, and brackets

        private string CorrectPassword; // The correct password the player is trying to guess
        private int RemainingAttempts; // Number of attempts left before lockout

        public event Action<GameStatus> OnGameEnded;
        public event Action<string> OnGameMessage; 
        public event Action<int> OnAttemptsUpdate; // Updates the UI. Pass the new attempts count.
        public event Action<string> OnBoardUpdate;

        private const string GarbageChars = "!@#$%^&*()_+-=[]{}|;:,.<>/?`~";
        private Random _random;

        private Dictionary<int, string> _wordLocationsDict = new Dictionary<int, string>();

        private int _currentActiveRows; // Tracks how many rows are currently active based on the number of words and board size. This can be used for UI scaling
        private GameConfiguration _config;
        private GameStatus _status = GameStatus.Playing;
        public HackingTerminal(DifficultyLevel difficulty = DifficultyLevel.Easy)
        {
            _random = new Random();
        }


        public void StartGame(DifficultyLevel difficulty)
        {
            _config = GameConfiguration.GetPreset(difficulty);

            // Reset on start
            _wordLocationsDict.Clear();
            _status = GameStatus.Playing; 
            RemainingAttempts = _config.StartingAttempts;

            // 1. Configuration
            var wordsToPlace = GetWords();
            CorrectPassword = SelectRandomPassword(wordsToPlace);

            // 2. Initialization
            _currentActiveRows = CalculateRequiredRows(wordsToPlace.Count, wordsToPlace[0].Length);
            int totalBoardSize = _currentActiveRows * _config.ColumnCount;

            char[] boardBuffer = GenerateRandomGarbage(totalBoardSize);
            // 3. Core Logic
            PlaceWordsOnBoard(boardBuffer, wordsToPlace);

            // 4. Finalization
            BoardState = new string(boardBuffer);

            // 5. Notify the UI
            SendMessage("Welcome to ROBCO Industries (TM) Termlink");
            SendMessage("Password Required");
            OnAttemptsUpdate?.Invoke(RemainingAttempts);
            OnBoardUpdate?.Invoke(BoardState);
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

     
        private List<string> GetWords()
        {
            // Set the word settings to the config settings
            int wordLength = _config.BaseWordLength;
            int wordCount = _config.WordCount;

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

            int rowsRequired = (int)Math.Ceiling((double)minSafeSize / _config.ColumnCount); 

            return Math.Max(_config.MinRowsCount, rowsRequired);
        }

        public void ProcessTurn(int index)
        {
            // If the game is over, ignore the click entirely.
            if (_status != GameStatus.Playing) return;

            SelectionResultDTO result = GetSelection(index);

            // Decides what to do based on the game rules
            if (result.IsValidSelection)
            {
                if (result.IsWord)
                {
                    CheckLikeness(result);
                }
                else
                {
                    HandleBracketBonus(result);
                }
            }
        }

        private void CheckLikeness(SelectionResultDTO selection)
        {
            if(_status != GameStatus.Playing)
                return;

            string guess = selection.SelectedText;
            int likeness = guess.Where((c, i) => i < CorrectPassword.Length && c == CorrectPassword[i]).Count();

            // NEW: BLL announces the result
            SendMessage($"You guessed: {guess}");
            SendMessage($"Likeness: {likeness}");

            // Check for win/loss here and announce it too
            if (likeness == CorrectPassword.Length)
            {
                EndGame(GameStatus.Won);
            }
            else
            {
                // now replace the guessed word with a dot (.)
                ApplyDudToBoard(selection.StartIndex, selection.Length);
                RemainingAttempts--;
                if (RemainingAttempts <= 0)
                {
                    EndGame(GameStatus.Lost);
                }
            }


            // Notify the UI that the number changed
            OnAttemptsUpdate?.Invoke(RemainingAttempts);
        }

        public SelectionResultDTO GetSelection(int index)
        {
            // Create a variable to hold our result so we can inject the status at the end
            SelectionResultDTO result;

            // 1. BOUNDARY CHECK
            if (index < 0 || index >= BoardState.Length)
            {
                result = new SelectionResultDTO { IsValidSelection = false };
            }
            else
            {
                // 2. CHECK IF IT IS A WORD
                var wordResult = CheckIfWord(index);
                if (wordResult != null)
                {
                    result = wordResult;
                }
                else
                {
                    // 3. CHECK IF IT IS A BRACKET PAIR
                    result = CheckForBracketPair(index);
                }
            }

            // CRITICAL: Attach the current game status from the BLL to the DTO
            result.Status = _status;
            return result;
        }

        // Pro-tip: Move the bracket loop to its own private helper to keep GetSelection readable
        private SelectionResultDTO CheckForBracketPair(int index)
        {
            char startChar = BoardState[index];

            if (IsStartBracket(startChar))
            {
                char expectedCloser = GetMatchingCloser(startChar);
                int maxDist = _config.BracketSearchDistance;

                for (int i = 1; i <= maxDist; i++)
                {
                    int checkIndex = index + i;
                    if (checkIndex >= BoardState.Length) break;

                    char checkChar = BoardState[checkIndex];

                    if (checkChar == expectedCloser)
                    {
                        return new SelectionResultDTO
                        {
                            StartIndex = index,
                            Length = i + 1,
                            SelectedText = BoardState.Substring(index, i + 1),
                            IsValidSelection = true,
                            IsWord = false
                        };
                    }
                    // if we hit a letter or digit, we stop searching (We don't count brackets that has a word between them)
                    if (char.IsLetterOrDigit(checkChar)) break;
                }
            }

            // Default if no bracket pair found
            return new SelectionResultDTO
            {
                StartIndex = index,
                Length = 1,
                SelectedText = startChar.ToString(),
                IsValidSelection = false
            };
        }

        public void HandleBracketBonus(SelectionResultDTO bracketInfo)
        {
            if (_status != GameStatus.Playing) return;

            // Wipe the bracket pair from the board
            ApplyDudToBoard(bracketInfo.StartIndex, bracketInfo.Length);
            // The "Manager" decides what happens
            if (_random.Next(4) == 0)
            {
                ResetAttempts();
            }
            else
            {
                RemoveRandomDud();
            }
        }

        private void RemoveRandomDud()
        {
            // 1. Find the target
            var availableDuds = _wordLocationsDict
                .Where(kvp => kvp.Value != CorrectPassword && !kvp.Value.StartsWith("."))
                .ToList();

            if (availableDuds.Any())
            {
                var target = availableDuds[_random.Next(availableDuds.Count)];

                // 2. Perform the surgical board update
                ApplyDudToBoard(target.Key, target.Value.Length);

                SendMessage("Dud Removed.");
            }
        }

        private void ResetAttempts()
        {
            RemainingAttempts = _config.StartingAttempts;
            OnAttemptsUpdate?.Invoke(RemainingAttempts);
            SendMessage("Attempts Restored.");
        }

        private void ApplyDudToBoard(int startIndex, int length)
        {
            // 1. Check if we are tracking a word at this index
            // If we are, update the dictionary.
            if (_wordLocationsDict.ContainsKey(startIndex))
            {
                _wordLocationsDict[startIndex] = new string('.', length);
            }
            // 1. Convert to a char array to bypass string immutability
            char[] buffer = BoardState.ToCharArray();

            // 2. Perform the surgical replacement using the specific index
            for (int i = 0; i < length; i++)
            {
                buffer[startIndex + i] = '.';
            }

            // 3. Rebuild the BoardState and notify the UI
            BoardState = new string(buffer);

            // This triggers the event you've already subscribed to in the UI!
            OnBoardUpdate?.Invoke(BoardState);
        }


        public TerminalSettingsDTO GetTerminalSettings()
        {
            // Safety: If game hasn't started, return 0 or calculate it manually
            int actualLength = BoardState != null ? BoardState.Length : 0;
            return new TerminalSettingsDTO
            {
                Columns = _config.ColumnCount,
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
                    if (word.StartsWith("."))
                    {
                        // If the word is a removed dud (starts with a dot), we don't want to select it
                        return null;
                    }

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

        private void EndGame(GameStatus status)
        {
            _status = status; // Update the game status
            OnGameEnded?.Invoke(_status); // Notify subscribers about the game end
            SendMessage(status == GameStatus.Won ? "Login Accepted!" : "ACCESS DENIED. TERMINAL LOCKED");
        }

        // This method is responsible for sending messages back to the UI or BLL
        private void SendMessage(string message)
        {

            OnGameMessage?.Invoke($"> {message}");
        }

        private bool IsStartBracket(char c)
        {

            return _config.BracketPairs.ContainsKey(c);
        }

        private char GetMatchingCloser(char c)
        {
            if (_config.BracketPairs.TryGetValue(c, out char closer))
            {
                return closer;
            }
            return '\0';
        }
    }
}
