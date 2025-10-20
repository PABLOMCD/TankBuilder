using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI_WPF.Behaviors
{
    public static class NumericDotInput
    {
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
                InputMethod.SetIsInputMethodEnabled(tb, false);
            }
            else
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                tb.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }
        }

        // Validación final: solo dígitos y UN punto (ya normalizado)
        private static readonly Regex _validPattern = new(@"^\d*\.?\d*$");

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;

            // Normalizar coma a punto
            string input = e.Text == "," ? "." : e.Text;

            // Construir texto propuesto e insertar manualmente
            int newCaret;
            string proposed = GetProposedText(tb, input, out newCaret);

            // Validar contra el patrón (punto ya normalizado)
            if (_validPattern.IsMatch(proposed))
            {
                e.Handled = true;              // nosotros hacemos la inserción
                tb.Text = proposed;
                tb.SelectionStart = newCaret;  // colocar el cursor después de lo insertado
                tb.SelectionLength = 0;
            }
            else
            {
                e.Handled = true; // bloquear entrada inválida
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Permitir edición/navegación normal
            if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Left or Key.Right or Key.Home or Key.End)
                return;

            // Ya no bloqueamos OemComma ni Decimal; los normalizamos en PreviewTextInput
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string paste = (string)e.DataObject.GetData(DataFormats.Text) ?? string.Empty;
            paste = paste.Replace(',', '.'); // normalizar

            int newCaret;
            string proposed = GetProposedText(tb, paste, out newCaret);

            if (_validPattern.IsMatch(proposed))
            {
                e.CancelCommand();            // hacemos la inserción nosotros
                tb.Text = proposed;
                tb.SelectionStart = newCaret;
                tb.SelectionLength = 0;
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Texto resultante y nueva posición del cursor si se inserta 'input' en la selección actual
        private static string GetProposedText(TextBox tb, string input, out int newCaret)
        {
            string text = tb.Text ?? string.Empty;
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            // Simular reemplazo de la selección por el input
            string baseText = selLen > 0 ? text.Remove(selStart, selLen) : text;
            string result = baseText.Insert(selStart, input);

            newCaret = selStart + input.Length;
            return result;
        }
    }
}
