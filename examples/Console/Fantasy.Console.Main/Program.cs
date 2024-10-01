using Fantasy;
using Fantasy.Console.Entity;
Fantasy.Log.Register(new ConsoleLog());
Fantasy.Platform.Console.Entry.Initialize(typeof(Fantasy.Console.Entity.Entry).Assembly);
Fantasy.Platform.Console.Entry.StartUpdate();
Entry.Show().Coroutine();
Console.ReadKey();