namespace SlimDXHelpers
{
    public class Context<ContainedType> : IContext<ContainedType>
    {
        public ContainedType Object { get; set; }
    }
}
