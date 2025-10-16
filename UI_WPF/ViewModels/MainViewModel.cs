using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using InventorBridge;

namespace UI_WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _title = "TankBuilder – Sheet Metal (in)";
        private double _widthIn;
        private double _heightIn;

        private string? _selectedStyle;
        private string? _selectedRule;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        // Entradas en pulgadas (in)
        public double WidthIn
        {
            get => _widthIn;
            set { _widthIn = value; OnPropertyChanged(); }
        }

        public double HeightIn
        {
            get => _heightIn;
            set { _heightIn = value; OnPropertyChanged(); }
        }

        // Listas y selección
        public ObservableCollection<string> SheetStyles { get; } = new();
        public ObservableCollection<string> UnfoldRules { get; } = new();

        public string? SelectedStyle
        {
            get => _selectedStyle;
            set { _selectedStyle = value; OnPropertyChanged(); }
        }

        public string? SelectedRule
        {
            get => _selectedRule;
            set { _selectedRule = value; OnPropertyChanged(); }
        }

        // Comandos
        public ICommand RefreshListsCommand { get; }
        public ICommand GenerateSheetCommand { get; }

        public MainViewModel()
        {
            // Valores ejemplo (in)
            WidthIn = 65.25;
            HeightIn = 82.98;

            RefreshListsCommand = new RelayCommand(RefreshLists);
            GenerateSheetCommand = new RelayCommand(GenerateSheet, CanGenerate);

            // Carga inicial
            RefreshLists();
        }

        private bool CanGenerate()
        {
            return WidthIn > 0 && HeightIn > 0;
        }

        private void RefreshLists()
        {
            try
            {
                var connector = new InventorConnector();

                SheetStyles.Clear();
                foreach (var s in connector.GetSheetMetalStyles())
                    SheetStyles.Add(s);
                SelectedStyle = SheetStyles.Count > 0 ? SheetStyles[0] : null;

                UnfoldRules.Clear();
                foreach (var r in connector.GetUnfoldRules())
                    UnfoldRules.Add(r);
                SelectedRule = UnfoldRules.Count > 0 ? UnfoldRules[0] : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudieron cargar estilos/reglas:\n{ex.Message}",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GenerateSheet()
        {
            try
            {
                // Construir modelo en pulgadas (el servicio/bridge convertirán a mm internamente)
                var service = new CoreLogic.Services.TankBuilderService(
                    WidthIn, HeightIn, /*Depth*/ 0, /*Thickness*/ 0);
                var tank = service.Tank;

                // Pasar selección al Bridge vía variables (simple y efectiva)
                if (!string.IsNullOrWhiteSpace(SelectedStyle))
                    Environment.SetEnvironmentVariable("VTC_INV_SM_STYLE", SelectedStyle);
                if (!string.IsNullOrWhiteSpace(SelectedRule))
                    Environment.SetEnvironmentVariable("VTC_INV_UNFOLD_RULE", SelectedRule);

                var connector = new InventorConnector();
                connector.CreateTankSheetMetal(tank);

                MessageBox.Show(
                    "Lámina generada en: C:\\Temp\\TankBuilder_SheetBase.ipt",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al generar la lámina:\n{ex.Message}",
                    "Falla", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Comando simple
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
    }
}
