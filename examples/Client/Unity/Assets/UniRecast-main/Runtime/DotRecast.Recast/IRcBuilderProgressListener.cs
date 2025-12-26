namespace DotRecast.Recast
{
    public interface IRcBuilderProgressListener
    {
        void OnProgress(int completed, int total);
    }
}