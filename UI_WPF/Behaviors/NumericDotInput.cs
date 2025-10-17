using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI_WPF.Behaviors
{
    public static class NumericDotInput
    {
        // Actívalo con: behaviors:NumericDotInput.IsEnabled="True"
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(NumericDotInput),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, bool value) =>
            element.SetValue(IsEnabledProperty, value);

        public static bool GetIsEnabled(DependencyObject element) =>
            (bool)element.GetValue(IsEnabledProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
            {
                tb.PreviewTextInput += OnPreviewTextInput;
                tb.PreviewKeyDown += OnPreviewKeyDown;
                DataObject.AddPastingHandler(tb, OnPaste);
                InputMethod.SetIsInputMethodEnabled(tb, false); // Evita IME (caracteres raros)
            }
            else
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                tb.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }
        }

        // Acepta: dígitos y un único punto; permite vacío mientras escriben
        private static readonly Regex _validPattern = new(@"^\d*\.?\d*$");

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            string proposed = GetProposedText(tb, e.Text);
            e.Handled = !_validPattern.IsMatch(proposed);
        }

        // Permitir teclas de edición/navegación. Bloquear separadores indeseados.
        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Left or Key.Right or Key.Home or Key.End)
                return;

            // Bloquea coma y combinaciones raras
            if (e.Key == Key.OemComma)
                e.Handled = true;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string pasteText = (string)e.DataObject.GetData(DataFormats.Text);
                string proposed = GetProposedText(tb, pasteText);
                if (!_validPattern.IsMatch(proposed))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Construye el texto resultante si se inserta 'input' en la selección actual
        private static string GetProposedText(TextBox tb, string input)
        {
            var text = tb.Text ?? string.Empty;
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            var baseText = selLen > 0 ? text.Remove(selStart, selLen) : text;
            return baseText.Insert(selStart, input);
        }
    }
}
