using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading
{
    public class AsyncBarrier
    {
        private readonly int m_participantCount;
        private readonly Func<AsyncBarrier, Task> m_postPhaseFunc;
        private int m_remainingParticipants;
        private ConcurrentStack<TaskCompletionSource<bool>> m_waiters;

        public AsyncBarrier(int participantCount) : this(participantCount, null)
        {
        }

        public AsyncBarrier(int participantCount, Func<AsyncBarrier, Task> postPhaseFunc)
        {
            if (participantCount <= 0) throw new ArgumentOutOfRangeException(nameof(participantCount));
            m_remainingParticipants = m_participantCount = participantCount;
            m_waiters = new ConcurrentStack<TaskCompletionSource<bool>>();
            m_postPhaseFunc = postPhaseFunc;
        }

        public TaskAwaiter GetAwaiter() => SignalAndWait().GetAwaiter();

        public Task SignalAndWait()
        {
            var tcs = new TaskCompletionSource<bool>();
            m_waiters.Push(tcs);
            if (Interlocked.Decrement(ref m_remainingParticipants) == 0)
            {
                m_remainingParticipants = m_participantCount;
                var waiters = m_waiters;
                m_waiters = new ConcurrentStack<TaskCompletionSource<bool>>();

                (m_postPhaseFunc?.Invoke(this) ?? Task.CompletedTask)
                    .ContinueWith((_) => Parallel.ForEach(waiters, w => w.SetResult(true)));
            }
            return tcs.Task;
        }
    }
}
