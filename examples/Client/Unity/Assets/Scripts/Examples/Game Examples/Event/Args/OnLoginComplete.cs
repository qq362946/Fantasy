namespace Fantasy
{
    public struct OnLoginComplete
    {
        public Scene Scene;

        public OnLoginComplete(Scene scene)
        {
            this.Scene = scene;
        }
    }
}