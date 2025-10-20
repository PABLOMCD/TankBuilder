using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private double _flangeIn;

        // Segmento elegido (segmento1..segmento4)
        private string _selectedSegmentKey = "segmento1";

        // Carpeta de origen (DEBEN existir los .ipt por segmento)
        private readonly string _sourceDirectory = @"C:\Users\pmcaj\E2X\TankBuilder\PIEZAPRUEBA\";

        // Carpeta de salida (aquí se guardan las piezas nuevas)
        private readonly string _outputDirectory = @"C:\Users\pmcaj\E2X\TankBuilder\Salida";

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

        public double FlangeIn
        {
            get => _flangeIn;
            set { _flangeIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Opciones visibles
        public ObservableCollection<string> SegmentOptions { get; } =
            new(new[] { "segmento1", "segmento2", "segmento3", "segmento4" });

        public string SelectedSegmentKey
        {
            get => _selectedSegmentKey;
            set
            {
                if (_selectedSegmentKey != value)
                {
                    _selectedSegmentKey = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PartPath));   // ruta origen
                    OnPropertyChanged(nameof(OutputPath)); // ruta destino
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Ruta origen calculada
        public string PartPath => Path.Combine(_sourceDirectory, $"{SelectedSegmentKey}.ipt");

        // Ruta destino calculada (misma convención de nombre)
        public string OutputPath => Path.Combine(_outputDirectory, $"{SelectedSegmentKey}.ipt");

        // Comandos
        public ICommand ModifyCommand { get; }

        public MainViewModel()
        {
            // Ejemplos iniciales
            WidthIn = 65.25;
            HeightIn = 82.98;
            FlangeIn = 4.00;

            ModifyCommand = new RelayCommand(ModifyPart, CanModify);
        }

        private bool CanModify()
        {
            return WidthIn > 0 && HeightIn > 0 && FlangeIn > 0
                   && File.Exists(PartPath);
        }

        private void ModifyPart()
        {
            try
            {
                if (!CanModify())
                {
                    var msg = File.Exists(PartPath)
                        ? "Verifique que ANCHO, LARGO y LARGOF sean > 0."
                        : $"No se encontró el archivo de origen:\n{PartPath}";
                    MessageBox.Show(msg, "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Construir modelo (en pulgadas). Depth = LARGOF por compatibilidad con tu TankModel.
                var service = new CoreLogic.Services.TankBuilderService(
                    WidthIn, HeightIn, /*Depth=LARGOF*/ FlangeIn, /*Thickness*/ 0);
                var tank = service.Tank;

                var connector = new InventorConnector();
                connector.ModifyTankSheetMetal(PartPath, tank, FlangeIn, _outputDirectory);

                MessageBox.Show(
                    $"Pieza modificada guardada en:\n{OutputPath}",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refrescar por si un watcher o validación visual depende de esto
                OnPropertyChanged(nameof(OutputPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al modificar/guardar la pieza:\n{ex.Message}",
                    "Falla", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // INotifyPropertyChanged
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
