using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using InventorBridge;
using Microsoft.Win32;

namespace UI_WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _title = "TankBuilder – Sheet Metal (in)";
        private double _widthIn;
        private double _heightIn;
        private double _flangeIn; // LARGOF (pestaña)
        private string? _partPath;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        // Entradas en pulgadas (in)
        public double WidthIn
        {
            get => _widthIn;
            set { _widthIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public double HeightIn
        {
            get => _heightIn;
            set { _heightIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // LARGOF = largo de pestaña (in)
        public double FlangeIn
        {
            get => _flangeIn;
            set { _flangeIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Ruta de la pieza (.ipt) a modificar
        public string? PartPath
        {
            get => _partPath;
            set { _partPath = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Comandos
        public ICommand BrowsePartCommand { get; }
        public ICommand GenerateSheetCommand { get; }   // "Modificar pieza"

        public MainViewModel()
        {
            // Ejemplos iniciales
            WidthIn = 65.25;
            HeightIn = 82.98;
            FlangeIn = 4.00;

            BrowsePartCommand = new RelayCommand(BrowsePart);
            GenerateSheetCommand = new RelayCommand(ModifyPart, CanModify);
        }

        private bool CanModify()
        {
            return WidthIn > 0 && HeightIn > 0 && FlangeIn > 0
                   && !string.IsNullOrWhiteSpace(PartPath)
                   && File.Exists(PartPath);
        }

        private void BrowsePart()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Selecciona la pieza de Sheet Metal a modificar",
                Filter = "Inventor Part (*.ipt)|*.ipt",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
                PartPath = dlg.FileName;
        }

        private void ModifyPart()
        {
            try
            {
                if (!CanModify())
                {
                    MessageBox.Show("Verifique ruta del archivo, y que ANCHO, LARGO y LARGOF sean > 0.",
                        "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Construir modelo (en pulgadas)
                var service = new CoreLogic.Services.TankBuilderService(
                    WidthIn, HeightIn, /*Depth=LARGOF*/ FlangeIn, /*Thickness*/ 0);
                var tank = service.Tank;

                var connector = new InventorConnector();

                // Modificar la pieza existente: ANCHO, LARGO y LARGOF (pulgadas)
                connector.ModifyTankSheetMetal(PartPath!, tank, FlangeIn);

                MessageBox.Show(
                    $"Pieza modificada:\n{PartPath}",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al modificar la pieza:\n{ex.Message}",
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
