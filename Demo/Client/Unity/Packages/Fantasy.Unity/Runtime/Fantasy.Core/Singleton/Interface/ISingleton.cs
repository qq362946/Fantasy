using System;
using System.Threading.Tasks;

namespace Fantasy.Helper
{
    public interface ISingleton : IDisposable
    {
        public bool IsDisposed { get; set; }
        public Task Initialize();
    }
}