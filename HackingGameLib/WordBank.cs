using System;
using System.Collections.Generic;
using System.Text;

namespace HackingGameLib
{
    public static class WordBank
    {
        private static List<string> _cachedWords;
        private const string FileName = "resources/WordBank.txt";
        private static char[] _delimiters = { ' ', '\n', '\r', '\t', '.', ',', '!', '?', '"', ':', ';', '(', ')', '[', ']' };
        /// <summary>
        /// Reads words from the text file and returns a random list of the specified length.
        /// </summary>
        public static List<string> GetWords(int length, int count)
        {
            // 1. LOAD: Read the file only once
            if (_cachedWords == null)
            {
                LoadWordsFromFile();
            }

            Random rnd = new Random();

            // 2. FILTER: Lambda to find words of correct length
            var validWords = _cachedWords
                .Where(w => w.Length == length)
                .Distinct() // Remove duplicates
                .ToList();

            // Safety check: If not enough words, return error placeholders
            if (validWords.Count < count)
            {
                // Fallback: If we can't find enough words in the file, pad with duplicates or placeholders
                return validWords.Concat(Enumerable.Repeat("ERROR", count)).Take(count).ToList();
            }

            // 3. SHUFFLE & SELECT
            return validWords.OrderBy(x => rnd.Next()).Take(count).ToList();
        }

        private static void LoadWordsFromFile()
        {
            

            if (File.Exists(FileName))
            {
                try
                {
                    // Read all text
                    string content = File.ReadAllText(FileName);

                    // Split by spaces, newlines, and punctuation
                   

                    _cachedWords = content
                        .Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)
                        .Select(w => w.ToUpper()) // Ensure consistent casing
                        .ToList();
                }
                catch (Exception ex)
                {
                    // Log error if you have a logger, or just initialize empty
                    _cachedWords = new List<string>();
                }
            }
            else
            {
                // File missing! 
                _cachedWords = new List<string> { "FILE", "MISSING", "CHECK", "SETUP" };
            }
        }
    }
}
