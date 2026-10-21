using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

public static class Bootstrap
{
    private static class States
    {
        public const int NotInitialized = 0;
        public const int Initializing = 1;
        public const int Initialized = 2;
        public const int Failed = 3;
    }

    private static int s_state;

    private static ExceptionDispatchInfo? s_initializationException;

    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref s_state, States.Initializing, States.NotInitialized) == States.NotInitialized)
        {
            try
            {
                System.Bootstrap.RegisterCheckAccessDelegate<DispatcherSynchronizationContext>(d =>
                    d.CheckAccess());
                Minimal.Mvvm.Bootstrap.RegisterCheckAccessDelegate<DispatcherSynchronizationContext>(d =>
                    d.CheckAccess());

                Volatile.Write(ref s_state, States.Initialized);
                return;
            }
            catch (Exception ex)
            {
                s_initializationException = ExceptionDispatchInfo.Capture(ex);
                Volatile.Write(ref s_state, States.Failed);

                var message = $"{typeof(Bootstrap).FullName}.{nameof(Initialize)}():{Environment.NewLine}{ex.Message}";
                Trace.WriteLine(message);
                Debug.Fail(message);
                throw;
            }
        }

        SpinWait spinWait = default;
        while (Volatile.Read(ref s_state) == States.Initializing)
        {
            spinWait.SpinOnce();
        }

        if (Volatile.Read(ref s_state) == States.Failed)
        {
            Debug.Assert(s_initializationException != null);
            s_initializationException!.Throw();
        }
    }
}