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
            _terminal.StartGame(difficulty: 1);

            // 4. Display the board
            TxtBoard.Text = _terminal.BoardState;
            UpdateStatus("Welcome to ROBCO Industries (TM) Termlink");
        }

        private void TxtBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cast sender to TextBox instead of TextBlock
            if (sender is TextBox textBox)
            {
                Point mousePos = e.GetPosition(textBox);

                // This method now exists! It returns the index of the clicked character.
                int charIndex = textBox.GetCharacterIndexFromPoint(mousePos, true);

                if (charIndex != -1)
                {
                    // Hand the index to the Logic Layer via our DTO
                    ProcessSelection(charIndex);
                }
            }
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