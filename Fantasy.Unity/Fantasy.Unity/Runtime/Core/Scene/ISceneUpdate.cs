namespace Fantasy
{
    public interface ISceneUpdate
    {
        void Update();
    }

    public sealed class EmptySceneUpdate : ISceneUpdate
    {
        public void Update()
        {
        
        }
    }
}