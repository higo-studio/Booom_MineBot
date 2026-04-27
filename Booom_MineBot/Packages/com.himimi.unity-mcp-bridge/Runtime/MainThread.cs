using System;
using System.Collections.Concurrent;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace McpBridge
{
    public sealed class MainThread
    {
        private static readonly TimeSpan DefaultQueueWaitTimeout = TimeSpan.FromSeconds(10);
        private readonly ConcurrentQueue<WorkItem> m_WorkItems = new();
        private int m_MainThreadId;

        public static MainThread Instance { get; } = new();

        private MainThread()
        {
            m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
#if UNITY_EDITOR
            EditorApplication.update -= Flush;
            EditorApplication.update += Flush;
#endif
        }

        public void Run(Action action)
        {
            Run(() =>
            {
                action();
                return true;
            });
        }

        public T Run<T>(Func<T> action)
        {
            if (Thread.CurrentThread.ManagedThreadId == m_MainThreadId)
            {
                return action();
            }

            var workItem = new WorkItem(action);
            m_WorkItems.Enqueue(workItem);
            if (!workItem.Wait(DefaultQueueWaitTimeout))
            {
                workItem.Cancel();
                throw new TimeoutException(
                    $"Timed out after {DefaultQueueWaitTimeout.TotalSeconds:0} seconds waiting for the Unity main thread. " +
                    "The editor is likely blocked by a modal dialog, a Play Mode transition, or another long-running request.");
            }

            return workItem.GetResult<T>();
        }

        private void Flush()
        {
            m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
            while (m_WorkItems.TryDequeue(out var workItem))
            {
                workItem.Execute();
            }
        }

        private sealed class WorkItem
        {
            private const int QueuedState = 0;
            private const int ExecutingState = 1;
            private const int CompletedState = 2;
            private const int CanceledState = 3;

            private readonly Func<object> m_Action;
            private readonly ManualResetEventSlim m_Completed = new(false);
            private Exception m_Exception;
            private object m_Result;
            private int m_State = QueuedState;

            public WorkItem(Delegate action)
            {
                m_Action = () => action.DynamicInvoke();
            }

            public void Execute()
            {
                if (Interlocked.CompareExchange(ref m_State, ExecutingState, QueuedState) != QueuedState)
                {
                    return;
                }

                try
                {
                    m_Result = m_Action();
                }
                catch (Exception exception)
                {
                    m_Exception = exception is System.Reflection.TargetInvocationException targetInvocationException &&
                                  targetInvocationException.InnerException != null
                        ? targetInvocationException.InnerException
                        : exception;
                }
                finally
                {
                    Interlocked.Exchange(ref m_State, CompletedState);
                    m_Completed.Set();
                }
            }

            public bool Wait(TimeSpan timeout)
            {
                return m_Completed.Wait(timeout);
            }

            public void Cancel()
            {
                if (Interlocked.CompareExchange(ref m_State, CanceledState, QueuedState) == QueuedState)
                {
                    m_Completed.Set();
                }
            }

            public T GetResult<T>()
            {
                if (m_Exception != null)
                {
                    throw m_Exception;
                }

                return m_Result is T value ? value : default;
            }
        }
    }
}
