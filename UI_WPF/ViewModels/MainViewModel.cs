using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using InventorBridge;


namespace UI_WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _title = "TankBuilder";
        private double _width;
        private double _height;
        private double _depth;
        private double _thickness;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public double Depth
        {
            get => _depth;
            set { _depth = value; OnPropertyChanged(); }
        }

        public double Thickness
        {
            get => _thickness;
            set { _thickness = value; OnPropertyChanged(); }
        }

        // Constructor
        public MainViewModel()
        {
            Width = 48;      // pulgadas
            Height = 60;
            Depth = 40;
            Thickness = 0.25;
            GenerateTankCommand = new RelayCommand(GenerateTank);
        }

        // Comando del botón
        public ICommand GenerateTankCommand { get; }

        private void GenerateTank()
        {
            try
            {
                // 1. Crea el modelo del tanque desde CoreLogic
                var service = new CoreLogic.Services.TankBuilderService(Width, Height, Depth, Thickness);
                var tank = service.Tank;

                // 2. Crea el conector a Inventor
                var connector = new InventorBridge.InventorConnector();

                // 3. Genera la pieza en Inventor
                connector.CreateTankPart(tank);

                // 4. Mensaje de confirmación
                MessageBox.Show(
                    "✅ Tanque generado correctamente en Inventor.\n\nArchivo guardado en:\nC:\\Temp\\TankBuilder_Sample.ipt",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"⚠️ Error al conectar con Inventor:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Clase auxiliar para comandos
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
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
