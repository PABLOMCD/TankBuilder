using CoreLogic.Models;
using System.Collections.Generic;

namespace CoreLogic.Services
{
    public class TankBuilderService
    {
        public TankModel Tank { get; private set; }
        public List<SegmentModel> Segments { get; private set; }

        public TankBuilderService(double width, double depth, double height, double wallThickness)
        {
            Tank = new TankModel(width, depth, height, wallThickness);
            Segments = new List<SegmentModel>
            {
                new SegmentModel(SegmentType.Front) { HasATC = true },
                new SegmentModel(SegmentType.Left) { HasMeterBox = true },
                new SegmentModel(SegmentType.Rear) { HasRadiator = true },
                new SegmentModel(SegmentType.Right) { HasDrainValve = true }
            };
        }

        public string Summary()
        {
            string info = $"Tank: {Tank}\n";
            foreach (var seg in Segments)
                info += $" - {seg}\n";

            return info;
        }
    }
}
