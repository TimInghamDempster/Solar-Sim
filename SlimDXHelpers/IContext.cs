namespace SlimDXHelpers
{
    public interface IContext <out ContainedType>
    {
        ContainedType Object { get; }
    }
}
