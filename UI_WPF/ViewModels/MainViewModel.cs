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

        private string _selectedSegmentKey = "BackWallTop";

        // Rutas fijas (D:)
        private readonly string _sourceDirectory = @"D:\E2X\PROGRAM\TankBuilder\PIEZAPRUEBA\";
        private readonly string _outputDirectory = @"D:\E2X\PROGRAM\TankBuilder\Salida\";

        // ✅ Nuevo: checkbox para aplicar color verde (SSTL) solo en BackWallTop
        private bool _applyGreen;

        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        // Entradas en pulgadas (in)
        public double WidthIn
        {
            get { return _widthIn; }
            set { _widthIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public double HeightIn
        {
            get { return _heightIn; }
            set { _heightIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public double FlangeIn
        {
            get { return _flangeIn; }
            set { _flangeIn = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Segmentos disponibles
        public ObservableCollection<string> SegmentOptions { get; } =
            new ObservableCollection<string>(new[] { "BackWallTop", "BackWallBottom", "FrontWallTop", "LeftWall" });

        public string SelectedSegmentKey
        {
            get { return _selectedSegmentKey; }
            set
            {
                if (_selectedSegmentKey != value)
                {
                    _selectedSegmentKey = value;
                    // Al cambiar segmento, recalcular rutas y visibilidad de checkbox
                    OnPropertyChanged();
                    OnPropertyChanged("PartPath");
                    OnPropertyChanged("OutputPath");
                    OnPropertyChanged("IsBackWallTop");

                    // Por claridad: apagar el checkbox al cambiar de segmento
                    ApplyGreen = false;

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // ✅ Visibilidad del checkbox (solo BackWallTop)
        public bool IsBackWallTop
        {
            get { return string.Equals(SelectedSegmentKey, "BackWallTop", StringComparison.OrdinalIgnoreCase); }
        }

        // ✅ Estado del checkbox
        public bool ApplyGreen
        {
            get { return _applyGreen; }
            set { _applyGreen = value; OnPropertyChanged(); }
        }

        // Rutas calculadas
        public string PartPath { get { return Path.Combine(_sourceDirectory, SelectedSegmentKey + ".ipt"); } }
        public string OutputPath { get { return Path.Combine(_outputDirectory, SelectedSegmentKey + ".ipt"); } }

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
                    string msg = File.Exists(PartPath)
                        ? "Verifique que ANCHO, LARGO y LARGOF sean > 0."
                        : "No se encontró el archivo de origen:\n" + PartPath;
                    MessageBox.Show(msg, "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Asegurar carpeta de salida
                if (!Directory.Exists(_outputDirectory))
                    Directory.CreateDirectory(_outputDirectory);

                // Construir modelo (en pulgadas). Depth = LARGOF
                var service = new CoreLogic.Services.TankBuilderService(
                    WidthIn, HeightIn, /*Depth=LARGOF*/ FlangeIn, /*Thickness*/ 0);
                var tank = service.Tank;

                var connector = new InventorConnector();

                // ✅ Apariencia opcional:
                // Solo si es BackWallTop y el checkbox está marcado => "Verde (SSTL)"
                string appearance = null;
                if (IsBackWallTop && ApplyGreen)
                    appearance = "Verde (SSTL)";

                connector.ModifyTankSheetMetal(PartPath, tank, FlangeIn, _outputDirectory, appearance);

                MessageBox.Show(
                    "Pieza modificada guardada en:\n" + OutputPath,
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged("OutputPath");
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                MessageBox.Show(
                    "No se pudo conectar con Inventor o aplicar cambios.\n\n" + comEx.Message,
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al modificar/guardar la pieza:\n" + ex.Message,
                    "Falla", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Comando simple
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) { return _canExecute == null || _canExecute(); }
        public void Execute(object parameter) { _execute(); }
    }
}
