using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Shared.Tp.St.Sync
{
    /// <summary>
    /// TODO: reimplement with cyclic buffer
    /// TODO: implement with ref accessors
    /// 
    /// </summary>
    public class HistoryQueue<T>
    {
        private readonly ConcurrentQueue<(int frame, T value)> _history = new();

        public int Count => _history.Count; // diagnostics

        public int FirstFrame => _history.Count > 0 ? _history.First().frame : 0;
        public int LastFrame => _history.Count > 0 ? _history.Last().frame : 0;

        public T LastValue
        {
            get
            {
                if (_history.Count > 0)
                    return _history.Last().value;
                throw new InvalidOperationException("Remote state is not received yet");
            }
        }

        public void ClearUntil(int frame)
        {
            while (_history.TryPeek(out var first) && first.frame < frame)
                _history.TryDequeue(out first);
        }

        public void AddValue(int frame, T value)
        {
            _history.Enqueue((frame, value));
        }
    }
}