using Fantasy;
using Fantasy.Console.Entity;

SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
Fantasy.Log.Register(new ConsoleLog());
Fantasy.Platform.Console.Entry.Initialize(typeof(Fantasy.Console.Entity.Entry).Assembly);
Fantasy.Platform.Console.Entry.StartUpdate();
Log.Debug($"{Thread.CurrentThread.ManagedThreadId} SynchronizationContext.Current:{SynchronizationContext.Current}");
Entry.Show().Coroutine();
Console.ReadKey();