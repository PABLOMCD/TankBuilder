namespace CoreLogic.Models
{
    public class TankModel
    {
        public double Width { get; set; }
        public double Depth { get; set; }
        public double Height { get; set; }
        public double WallThickness { get; set; }

        public TankModel(double width, double depth, double height, double wallThickness)
        {
            Width = width;
            Depth = depth;
            Height = height;
            WallThickness = wallThickness;
        }

        public override string ToString()
        {
            return $"Tank {Width}x{Depth}x{Height} mm - Thickness {WallThickness} mm";
        }
    }
}
