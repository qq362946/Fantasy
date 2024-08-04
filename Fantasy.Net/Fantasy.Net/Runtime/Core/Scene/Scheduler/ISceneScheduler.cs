using System;

namespace Fantasy
{
    public interface ISceneScheduler : IDisposable
    {
        void Add(Scene scene);
        void Remove(Scene scene);
        void Update();
    }
}