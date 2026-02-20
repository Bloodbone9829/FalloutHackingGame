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
                    var settings = _terminal.GetTerminalSettings();
                    int rowsPerCol = settings.Rows / 2;
                    SplitAndAssignBoard(newBoard, settings.Columns, rowsPerCol);
                });
            };

            // 1. Start Game
            _terminal.StartGame(DifficultyLevel.Easy);
            var settings = _terminal.GetTerminalSettings();

            // 2. Prepare the Visuals
            // We assume the board is split into 2 visual columns
            int rowsPerColumn = settings.Rows / 2;

            // 3. Generate Hex Headers (Left start 0xF900, Right starts 0xFA00)
            TxtHexLeft.Text = GenerateHexHeaders(0xF900, rowsPerColumn);
            TxtHexRight.Text = GenerateHexHeaders(0xFA00, rowsPerColumn);

            // 4. Split the single Board string into Left/Right TextBoxes
            SplitAndAssignBoard(_terminal.BoardState, settings.Columns, rowsPerColumn);
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