using CoreLogic.Models;
using System.Text;

namespace CoreLogic.Services
{
    public class TankBuilderService
    {
        public TankModel Tank { get; private set; }

        public TankBuilderService(double width, double height, double depth, double thickness)
        {
            Tank = new TankModel(width, height, depth, thickness);
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Tank Summary (in):");
            sb.AppendLine($" - Width: {Tank.Width}");
            sb.AppendLine($" - Height: {Tank.Height}");
            sb.AppendLine($" - Depth: {Tank.Depth}");
            sb.AppendLine($" - Thickness: {Tank.Thickness}");
            sb.AppendLine();
            sb.AppendLine("In millimeters:");
            sb.AppendLine($" - Width: {Tank.Width * 25.4:F2} mm");
            sb.AppendLine($" - Height: {Tank.Height * 25.4:F2} mm");
            sb.AppendLine($" - Depth: {Tank.Depth * 25.4:F2} mm");
            sb.AppendLine($" - Thickness: {Tank.Thickness * 25.4:F2} mm");
            return sb.ToString();
        }
    }
}
