namespace CoreLogic.Models
{
    public class TankModel
    {
        public double Width { get; set; }        // pulgadas
        public double Height { get; set; }       // pulgadas
        public double Depth { get; set; }        // pulgadas
        public double Thickness { get; set; }    // pulgadas

        public TankModel(double width, double height, double depth, double thickness)
        {
            Width = width;
            Height = height;
            Depth = depth;
            Thickness = thickness;
        }

        public override string ToString()
        {
            return $"Tank: {Width}x{Depth}x{Height} in | Thickness: {Thickness} in";
        }
    }
}
