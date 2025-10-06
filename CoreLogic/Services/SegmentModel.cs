namespace CoreLogic.Models
{
    public enum SegmentType
    {
        Front = 1,
        Left = 2,
        Rear = 3,
        Right = 4
    }

    public class SegmentModel
    {
        public SegmentType Type { get; set; }
        public bool HasRadiator { get; set; }
        public bool HasATC { get; set; }
        public bool HasMeterBox { get; set; }
        public bool HasDrainValve { get; set; }

        public SegmentModel(SegmentType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return $"{Type} Segment | ATC:{HasATC}, Radiator:{HasRadiator}, MeterBox:{HasMeterBox}, Drain:{HasDrainValve}";
        }
    }
}
