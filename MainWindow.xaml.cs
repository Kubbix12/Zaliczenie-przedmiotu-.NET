using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CrudWpfApp
{
    public partial class MainWindow : Window
    {
        private static readonly Regex DigitsOnly = new Regex("^[0-9]+$");
        private static readonly Regex PriceAllowed = new Regex("^[0-9]+([.,][0-9]*)?$");

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Stock_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsOnly.IsMatch(e.Text);
        }

        private void Price_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb)
            {
                e.Handled = true;
                return;
            }

            var next = GetNextText(tb, e.Text);
            e.Handled = !IsValidPriceText(next);
        }

        private void Stock_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var paste = e.DataObject.GetData(DataFormats.Text) as string ?? "";
            if (!DigitsOnly.IsMatch(paste))
                e.CancelCommand();
        }

        private void Price_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb)
            {
                e.CancelCommand();
                return;
            }

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var paste = e.DataObject.GetData(DataFormats.Text) as string ?? "";
            var next = GetNextText(tb, paste);

            if (!IsValidPriceText(next))
                e.CancelCommand();
        }

        private static bool IsValidPriceText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            return PriceAllowed.IsMatch(text);
        }

        private static string GetNextText(TextBox tb, string input)
        {
            var current = tb.Text ?? "";
            var selectionStart = tb.SelectionStart;
            var selectionLength = tb.SelectionLength;

            if (selectionLength > 0)
                current = current.Remove(selectionStart, selectionLength);

            return current.Insert(selectionStart, input);
        }
    }
}
