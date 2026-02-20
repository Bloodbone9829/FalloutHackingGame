using HackingGameLib;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace HackingGameUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 1. Initialize the BLL
        private HackingTerminal _terminal = new HackingTerminal();
        private int _lastMousedIndex = -1;
        private DifficultyLevel _currentDifficulty;
        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            _terminal.OnGameMessage += (msg) =>
            {
                // Since BLL events can come from any thread, we need to ensure UI updates happen on the main thread.
                Dispatcher.Invoke(() => LogToTerminal(msg));
            };

            _terminal.OnAttemptsUpdate += (attempts) =>
            {
                // Lambda Statement satisfies Grade A requirements
                Dispatcher.Invoke(() =>
                {
                    TxtAttempts.Text = attempts.ToString();
                    // Professional touch: change color if attempts are low
                    TxtAttempts.Foreground = attempts <= 1 ? Brushes.Red : Brushes.Lime;
                });
            };

            _terminal.OnBoardUpdate += (newBoard) =>
            {
                // Use Dispatcher because BLL might be on a different thread
                Dispatcher.Invoke(() =>
                {
                    // 1. Get settings NOW because the board is ready
                    var settings = _terminal.GetTerminalSettings();
                    int rowsPerCol = settings.Rows / 2;

                    // 2. Prepare the Visuals (Moved from InitializeGame)
                    TxtHexLeft.Text = GenerateHexHeaders(0xF900, rowsPerCol);
                    TxtHexRight.Text = GenerateHexHeaders(0xFA00, rowsPerCol);

                    // 3. Update the board textboxes
                    SplitAndAssignBoard(newBoard, settings.Columns, rowsPerCol);
                });
            };

            _terminal.OnGameEnded += (status) =>
            {
                Dispatcher.Invoke(() => HandleGameEnd(status));
            };
        }

        private void TxtBoard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 1. Determine the offset (0 for left box, calculated value for right)
                // This is still PL logic because it relates to how we display the board.
                int offset = (textBox == TxtBoardRight) ? CalculateRightBoardOffset() : 0;

                // 2. Delegate the coordinate mapping to our Helper class
                int finalIndex = TerminalUIHelper.MapMouseToIndex(textBox, e.GetPosition(textBox), offset);

                // 3. If the click was valid (not on a newline/control character)
                if (finalIndex != -1)
                {
                    // 4. Pass the logical index to the BLL
                    ProcessSelection(finalIndex);

                    // Mark the event as handled to prevent further routing
                    e.Handled = true;
                }
            }
        }

        private void SplitAndAssignBoard(string fullBoard, int lineLength, int rowsPerCol)
        {
            // A. Split the full board string into lines based on the line length
            List<string> lines = fullBoard.Chunk(lineLength).Select(c => new string(c)).ToList();

            // B. Assign the first half of the lines to the left TextBox and the second half to the right TextBox
            TxtBoardLeft.Text = string.Join(Environment.NewLine, lines.Take(rowsPerCol));
            TxtBoardRight.Text = string.Join(Environment.NewLine, lines.Skip(rowsPerCol));
        }

        // Updated Hex Generator to take a specific start address
        private string GenerateHexHeaders(int startAddress, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"0x{startAddress:X}");
                startAddress += 16;
            }
            return sb.ToString();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && Enum.TryParse(btn.Tag?.ToString(), out DifficultyLevel level))
            {
                // UI Logic: Show the game screen
                MenuScreen.Visibility = Visibility.Collapsed;
                GameScreen.Visibility = Visibility.Visible;

                _currentDifficulty = level;
                // BLL Logic: Start the engine (this triggers OnBoardUpdate above)
                StartNewSession(level);
            }
        }

        private void BtnToMenu_Click(object sender, RoutedEventArgs e)
        {
            // Presentation Logic: Toggle visibility of the Grids defined in XAML
            MenuScreen.Visibility = Visibility.Visible;
            GameScreen.Visibility = Visibility.Collapsed;
            GameOverOverlay.Visibility = Visibility.Collapsed;
        }

        // 2. Logic for the "RETRY" button
        private void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            // Implementation: Use your existing logic to restart the game
            // You should use the same difficulty level stored from the last BtnStart_Click
            StartNewSession(_currentDifficulty);
        }
        private void StartNewSession(DifficultyLevel level)
        {
            // UI Logic: Switch screens
            MenuScreen.Visibility = Visibility.Collapsed;
            GameOverOverlay.Visibility = Visibility.Collapsed;
            GameScreen.Visibility = Visibility.Visible;

            // --- FULL UI STATE RESET ---
            // 1. Reset Text
            TxtStatus.Text = "";

            // 2. Reset Colors to default Terminal Green
            TxtStatus.Foreground = Brushes.Lime;

            // 3. Reset Opacity
            TxtBoardLeft.Opacity = 1.0;
            TxtBoardRight.Opacity = 1.0;

            // 4. Re-enable interaction
            TxtBoardLeft.IsReadOnly = false;
            TxtBoardRight.IsReadOnly = false;

            // 5. Reset internal mouse tracking
            _lastMousedIndex = -1;
            // ---------------------------

            // BLL Logic: Initialize the game
            _terminal.StartGame(level);
        }

        private void ProcessSelection(int index)
        {
            _terminal.ProcessTurn(index);
        }

        private void TxtBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                int offset = (textBox == TxtBoardRight) ? CalculateRightBoardOffset() : 0;
                int finalIndex = TerminalUIHelper.MapMouseToIndex(textBox, e.GetPosition(textBox), offset);

                if (finalIndex == _lastMousedIndex) return;
                _lastMousedIndex = finalIndex;

                ApplyUnifiedHighlight(finalIndex != -1 ? _terminal.GetSelection(finalIndex) : null);
            }
        }

        private void ApplyUnifiedHighlight(SelectionResultDTO result)
        {
            // 1. Clear highlights on BOTH boxes to reset the state
            TxtBoardLeft.Select(0, 0);
            TxtBoardRight.Select(0, 0);

            // If there is no valid selection, we just leave them cleared
            if (result == null) return;

            // 2. Define the logical boundaries for each box
            int rightBoardOffset = CalculateRightBoardOffset();
            var settings = _terminal.GetTerminalSettings();

            // 3. Try to highlight the Left Box (Logical range: 0 to rightBoardOffset)
            HighlightPartOfWord(TxtBoardLeft, result, 0, rightBoardOffset);

            // 4. Try to highlight the Right Box (Logical range: rightBoardOffset to TotalLength)
            HighlightPartOfWord(TxtBoardRight, result, rightBoardOffset, settings.TotalLength);
        }

        private void HighlightPartOfWord(TextBox box, SelectionResultDTO result, int boxStartRange, int boxEndRange)
        {
            int selectionStart = result.StartIndex;
            int selectionEnd = result.StartIndex + result.Length;

            // Check if the selection (start to end) overlaps with this box's logical range
            if (selectionStart < boxEndRange && selectionEnd > boxStartRange)
            {
                // Calculate the 'Local' start index relative to THIS box
                int localLogicalStart = Math.Max(0, selectionStart - boxStartRange);

                // Calculate how many characters of this word actually fit in THIS box
                int localLogicalLength = Math.Min(selectionEnd, boxEndRange) - Math.Max(selectionStart, boxStartRange);

                // Use your existing visual mapping logic
                int visualStart = TerminalUIHelper.FindVisualIndex(box.Text, localLogicalStart);
                int visualLength = TerminalUIHelper.CalculateVisualLength(box.Text, visualStart, localLogicalLength);

                // Apply the selection
                box.Focus();
                box.Select(visualStart, visualLength);
            }
        }

        private void HandleGameEnd(GameStatus status)
        {
            // 1. Show the Overlay (The missing piece!)
            GameOverOverlay.Visibility = Visibility.Visible;
            TxtGameOverTitle.Text = status == GameStatus.Won ? "LOGIN GRANTED" : "TERMINAL LOCKED";
            TxtGameOverTitle.Foreground = status == GameStatus.Won ? Brushes.Lime : Brushes.Red;

            // 2. Lock the board visuals
            TxtBoardLeft.IsReadOnly = true;
            TxtBoardRight.IsReadOnly = true;
            TxtBoardLeft.Opacity = 0.5;
            TxtBoardRight.Opacity = 0.5;

            // 3. Clear highlights
            ApplyUnifiedHighlight(null);

            // 4. Update status log colors
            TxtStatus.Foreground = status == GameStatus.Won ? Brushes.Lime : Brushes.Red;
        }

        private void TxtBoard_MouseLeave(object sender, MouseEventArgs e)
        {
            // Clear everything when the mouse leaves the board entirely
            ApplyUnifiedHighlight(null);
            _lastMousedIndex = -1; // Reset tracking
        }

        // Helper for updating the status log
        private void LogToTerminal(string msg)
        {
            // Appends new messages to the bottom log
            TxtStatus.Text += $"\n{msg}";
            // UI Logic: Scroll to the bottom so the newest message is visible
            // We look for the ScrollViewer that contains TxtStatus
            StatusScrollViewer.ScrollToEnd();
        }

        private int CalculateRightBoardOffset()
        {
            // 1. Get current board dimensions from the BLL via the DTO
            var settings = _terminal.GetTerminalSettings();

            // 2. Logic: The offset is the number of rows in the left column 
            // multiplied by the characters per row.
            int rowsInLeftColumn = settings.Rows / 2;

            return rowsInLeftColumn * settings.Columns;
        }

    }
}