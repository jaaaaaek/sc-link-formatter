using Avalonia.Controls;
using Avalonia.Input;

namespace LinkFormatter.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (FocusManager?.GetFocusedElement() is TextBox && e.Source is not TextBox)
            {
                FocusManager?.ClearFocus();
            }
        }
    }
}
