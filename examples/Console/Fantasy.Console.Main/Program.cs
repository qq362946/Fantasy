// See https://aka.ms/new-console-template for more information

using Fantasy;
using Fantasy.Console.Entity;

Fantasy.Log.Register(new ConsoleLog());
Fantasy.Platform.Console.Entry.Initialize(typeof(Fantasy.Console.Entity.Entry).Assembly);
Thread.Sleep(3000);
await Entry.Show();