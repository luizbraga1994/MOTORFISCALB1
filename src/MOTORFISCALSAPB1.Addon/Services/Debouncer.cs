using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MOTORFISCALSAPB1.Addon.Services
{
    /// <summary>
    /// Debouncer por chave — cancela execuções anteriores e agenda a última.
    /// Usado para evitar chamadas fiscais repetidas enquanto o usuário digita.
    /// </summary>
    public sealed class Debouncer
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, CancellationTokenSource> _pending = new Dictionary<string, CancellationTokenSource>();
        private readonly int _delayMs;

        public Debouncer(int delayMilliseconds)
        {
            _delayMs = delayMilliseconds;
        }

        public void Schedule(string key, Func<CancellationToken, Task> action)
        {
            CancellationTokenSource cts;
            lock (_sync)
            {
                if (_pending.TryGetValue(key, out var existing))
                {
                    existing.Cancel();
                    existing.Dispose();
                }
                cts = new CancellationTokenSource();
                _pending[key] = cts;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_delayMs, cts.Token).ConfigureAwait(false);
                    await action(cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException) { }
                finally
                {
                    lock (_sync)
                    {
                        if (_pending.TryGetValue(key, out var current) && current == cts)
                        {
                            _pending.Remove(key);
                        }
                    }
                    cts.Dispose();
                }
            });
        }
    }
}
