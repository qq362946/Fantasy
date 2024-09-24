namespace Fantasy
{
    internal interface ISceneUpdate
    {
        void Update();
    }

    internal sealed class EmptySceneUpdate : ISceneUpdate
    {
        public void Update()
        {
        
        }
    }
}