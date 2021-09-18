using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace UpToYou.Client.Wpf
{
    /// <summary>
    /// Interaction logic for UpdatesView.xaml
    /// </summary>
    public partial class UpdatesView : UserControl
    {
        public UpdatesView()
        {
            InitializeComponent();

            this.CommandBindings.Add(new CommandBinding(
                command:Markdig.Wpf.Commands.Hyperlink,
                HyperlinkExecute,
                HyperlinkCanExecute));
        }

        private void HyperlinkCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void HyperlinkExecute(object sender, ExecutedRoutedEventArgs e) {
            var hyperlink = (Hyperlink)e.OriginalSource;
            try {
                Process.Start(hyperlink.NavigateUri.AbsoluteUri);
            }
            catch{ }
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
