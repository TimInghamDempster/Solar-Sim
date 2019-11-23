namespace SlimDXHelpers
{
    public struct MemorySize
    {
        public int SizeInBits { get; }

        public int RequiredBytes => SizeInBits / 8;

        public static MemorySize operator * (MemorySize memorySize, ItemCount count)
        {
            var newSize = new MemorySize(memorySize.SizeInBits * count.Count);
            return newSize;
        }

        private MemorySize(int sizeInBits)
        {
            SizeInBits = sizeInBits;
        }

        public static MemorySize FromBits(int numberOfBits)
        {
            return new MemorySize(numberOfBits);
        }
    }
}
