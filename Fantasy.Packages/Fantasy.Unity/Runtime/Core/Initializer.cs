using System;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy
{
    public static class Initializer
    {
#if FANTASY_NET
        /// <summary>
        /// Bson初始化的事件
        /// </summary>
        public static Action OnBsonInitialize;
        /// <summary>
        /// MongoDB 初始化自定义委托，当设置了这个委托后，就不会自动创建MongoClient，把创建权交给自定义。
        /// </summary>
        public static Func<Fantasy.Database.DataBaseCustomConfig, MongoDB.Driver.MongoClient>? MongoDbCustomInitialize;
#endif
        /// <summary>
        /// MemoryPack初始化的事件
        /// </summary>
        public static Action OnMemoryPackInitialize;
    }
}


