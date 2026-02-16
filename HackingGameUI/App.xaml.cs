using System.Configuration;
using System.Data;
using System.Windows;

namespace HackingGameUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // FIX: This switch forces WPF to render selection text colors correctly 
            // instead of using the default "Adorner" layer which ignores the brush.
            AppContext.SetSwitch("Switch.System.Windows.Controls.Text.UseAdornerForTextboxSelectionRendering", false);
        }
    }

}
