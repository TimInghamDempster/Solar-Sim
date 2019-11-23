namespace SlimDXHelpers
{
    public class ItemCount
    {
        public int Count { get; }
        public ItemCount(int count)
        {
            Count = count;
        }

        public override string ToString() => Count.ToString();
    }

    public class ItemCount<ItemType> : ItemCount
    {
        public ItemCount(int count) : base(count)
        {

        }
    }
}
