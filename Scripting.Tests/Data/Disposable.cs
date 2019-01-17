using System;

namespace Scripting.Tests.Data {
    public class Disposable : IDisposable {
        public bool Disposed { get; set; }
        public void Dispose() {
            Disposed = true;
        }
    }
}