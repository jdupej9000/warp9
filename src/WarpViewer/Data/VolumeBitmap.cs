namespace Warp9.Data
{
    public class VolumeBitmap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public int RowStrideBytes { get; private set; }
        public int SliceStrideBytes { get; private set; }
        public int BytesPerSample { get; private set; }
        public int SizeBytes => SliceStrideBytes * Depth * BytesPerSample;
    }
}
