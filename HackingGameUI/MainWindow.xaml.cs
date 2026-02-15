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
            // 2. Subscribe to events (Delegates)
            _terminal.OnGameMessage += HandleGameMessage;
            _terminal.OnAttemptsUpdate += HandleAttemptsUpdate;

            // 3. Start the game logic
            _terminal.StartGame();

            var settings = _terminal.GetTerminalSettings();

            TxtBoard.Width = MeasureStringWidth(TxtBoard, settings.Columns);

            // 4. Display the board
            TxtBoard.Text = _terminal.BoardState;
            TxtHexCodes.Text = GenerateHexHeaders(settings.Rows);
            UpdateStatus("Welcome to ROBCO Industries (TM) Termlink");
        }

        private void TxtBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 5. Handle user clicks on the board
            if (sender is TextBox textBox)
            {
                Point mousePos = e.GetPosition(textBox);

                //  returns the index of the clicked character.
                int charIndex = textBox.GetCharacterIndexFromPoint(mousePos, true);

                if (charIndex != -1)
                {
                    // Hand the index to the Logic Layer via our DTO
                    ProcessSelection(charIndex);
                }
            }
        }

        private double MeasureStringWidth(TextBox target, int charCount)
        {
            // Create a string of 'X's to measure
            string testString = new string('X', charCount);

            var formattedText = new FormattedText(
                testString,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(target.FontFamily, target.FontStyle, target.FontWeight, target.FontStretch),
                target.FontSize,
                Brushes.Black, // Color doesn't matter for measurement
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Add a tiny bit of padding (5-10px) to prevent accidental wrapping
            return formattedText.Width + 10;
        }

        private string GenerateHexHeaders(int rowCount)
        {
            StringBuilder sb = new StringBuilder();
            // Fallout terminals usually start at memory address 0xF900 or similar
            int startAddress = 0xF900;

            for (int i = 0; i < rowCount; i++)
            {
                // Format as Hex "0xF900"
                sb.AppendLine($"0x{startAddress:X}");
                startAddress += 16; // Increment address (visual only)
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