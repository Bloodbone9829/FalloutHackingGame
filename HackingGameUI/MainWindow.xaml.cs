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

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            _terminal.OnGameMessage += HandleGameMessage;
            _terminal.OnAttemptsUpdate += HandleAttemptsUpdate;
            // 1. Start Game
            _terminal.StartGame(difficulty: 1);
            var settings = _terminal.GetTerminalSettings();

            // 2. Prepare the Visuals
            // We assume the board is split into 2 visual columns
            int rowsPerColumn = settings.Rows / 2;

            // 3. Generate Hex Headers (Left start 0xF900, Right starts 0xFA00)
            TxtHexLeft.Text = GenerateHexHeaders(0xF900, rowsPerColumn);
            TxtHexRight.Text = GenerateHexHeaders(0xFA00, rowsPerColumn);

            // 4. Split the single Board string into Left/Right TextBoxes
            SplitAndAssignBoard(_terminal.BoardState, settings.Columns, rowsPerColumn);

            UpdateStatus("Welcome to ROBCO Industries (TM) Termlink");
        }

        private void TxtBoard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 1. Get Visual Index
                // This WPF method handles font sizes and variable widths for us.
                int uiIndex = textBox.GetCharacterIndexFromPoint(e.GetPosition(textBox), true);

                // Safety Check: Ensure index is valid
                if (uiIndex < 0 || uiIndex >= textBox.Text.Length) return;

                // If we clicked a newline character or whitespace, ignore it.
                char clickedChar = textBox.Text[uiIndex];
                if (char.IsControl(clickedChar)) return;


                // This ignores \r, \n, or any other formatting fluff completely.
                string textPrecedingClick = textBox.Text.Substring(0, uiIndex);

                // Count only non-control characters (letters, numbers, symbols)
                int localBllIndex = textPrecedingClick.Count(c => !char.IsControl(c));

                // 4. Apply Column Offset
                // Logic: If we are on the right board, add the total size of the left board.
                int offset = 0;
                if (textBox == TxtBoardRight)
                {
                    var settings = _terminal.GetTerminalSettings();
                    int rowsInLeftCol = settings.Rows / 2;
                    offset = rowsInLeftCol * settings.Columns;
                }

                // 5. Execute
                ProcessSelection(localBllIndex + offset);
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
            // Ask the BLL for the data at this index
            SelectionResultDTO result = _terminal.GetSelection(index);

            if (result.IsValidSelection)
            {
                UpdateStatus($"Selected: {result.SelectedText}");

                // If it's a word, trigger the likeness logic in the BLL
                if (result.IsWord)
                {
                    int likeness = _terminal.CheckLikeness(result.SelectedText);
                    UpdateStatus($"Likeness: {likeness}");
                }
            }
        }
        // Helper for updating the status log
        private void UpdateStatus(string msg)
        {
            // Appends new messages to the bottom log
            TxtStatus.Text = $"> {msg}\n" + TxtStatus.Text;
        }

        // Event Handlers for the BLL events
        private void HandleGameMessage(string msg) => UpdateStatus(msg);
        private void HandleAttemptsUpdate(int attempts) => TxtAttempts.Text = attempts.ToString();
    }
}