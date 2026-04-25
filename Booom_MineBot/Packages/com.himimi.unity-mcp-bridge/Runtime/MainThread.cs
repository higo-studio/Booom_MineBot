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
            workItem.Wait();
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
            private readonly Func<object> m_Action;
            private readonly ManualResetEventSlim m_Completed = new(false);
            private Exception m_Exception;
            private object m_Result;

            public WorkItem(Delegate action)
            {
                m_Action = () => action.DynamicInvoke();
            }

            public void Execute()
            {
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
                    m_Completed.Set();
                }
            }

            public void Wait()
            {
                m_Completed.Wait();
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
