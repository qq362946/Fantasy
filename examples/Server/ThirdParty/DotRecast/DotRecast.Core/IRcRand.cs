namespace DotRecast.Core
{
    public interface IRcRand
    {
        float Next();
        double NextDouble();
        int NextInt32();
    }
}