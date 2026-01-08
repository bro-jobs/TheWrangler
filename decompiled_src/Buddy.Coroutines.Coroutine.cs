#region Assembly RebornBuddy, Version=1.0.802.0, Culture=neutral, PublicKeyToken=48d7174f8a943034
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using nmSrPPd33X4DJvagbX;
using Xy9Ac91TTPsPd5mC4M;

namespace Buddy.Coroutines;

//
// Summary:
//     Represents a coroutine.
public sealed class Coroutine : IDisposable
{
    private class ModularServiceEngine : INotifyCompletion
    {
        [CompilerGenerated]
        private bool m_IsPredictorResponder;

        public bool IsCompleted => false;

        [SpecialName]
        [CompilerGenerated]
        public bool AnalyzeGroupedContext()
        {
            return m_IsPredictorResponder;
        }

        [SpecialName]
        [CompilerGenerated]
        public void SpecifyStaticMap(bool cantask)
        {
            m_IsPredictorResponder = cantask;
        }

        public ModularServiceEngine GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            if (AnalyzeGroupedContext())
            {
                throw CanceledException();
            }
        }

        public void OnCompleted(Action continuation)
        {
            Current._ExternalToken = continuation;
        }

        public ModularServiceEngine()
        {
            LocalServerSender.ExecuteMixedValue();
            base._002Ector();
        }

        static ModularServiceEngine()
        {
            AnalyzerStrategy.uc8lngiwB();
        }
    }

    [CompilerGenerated]
    private sealed class _003C_003Ec__DisplayClass3_0
    {
        public Func<Task> engineProxyTransaction;

        public _003C_003Ec__DisplayClass3_0()
        {
            LocalServerSender.ExecuteMixedValue();
            base._002Ector();
        }

        internal async Task<object> _003CReturnsNullWrapper_003Eb__0()
        {
            await engineProxyTransaction();
            return null;
        }

        static _003C_003Ec__DisplayClass3_0()
        {
            AnalyzerStrategy.uc8lngiwB();
        }
    }

    [CompilerGenerated]
    private sealed class _003C_003Ec__DisplayClass5_0
    {
        public Func<Task<object>> m_IdentifiableFilterEngine;

        public Coroutine _EngineTransactionThread;

        public _003C_003Ec__DisplayClass5_0()
        {
            LocalServerSender.ExecuteMixedValue();
            base._002Ector();
        }

        internal void _003C_002Ector_003Eb__0()
        {
            Task<object> task;
            try
            {
                task = m_IdentifiableFilterEngine();
            }
            catch (Exception innerException)
            {
                throw _EngineTransactionThread.SetException(new CoroutineUnhandledException(AnalyzerStrategy.LOUDD90ML(0x2634D94E ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_2549d0211e4a4cf8ac702f3a421be6a2), innerException));
            }

            if (task == null)
            {
                throw _EngineTransactionThread.SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(-54770918 ^ -1613697959 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_8e24f8b4bca241acb0e1a3a906695a09)));
            }

            _EngineTransactionThread._AnnotationReporter = task;
        }

        static _003C_003Ec__DisplayClass5_0()
        {
            AnalyzerStrategy.uc8lngiwB();
        }
    }

    private readonly ModularServiceEngine _VisibleDriverSummarizer;

    private Task<object> _AnnotationReporter;

    private Action _ExternalToken;

    [ThreadStatic]
    private static Coroutine summarizerToken;

    [CompilerGenerated]
    private CoroutineStatus alphabeticRole;

    [CompilerGenerated]
    private bool isSummarizerTransaction;

    [CompilerGenerated]
    private CoroutineException groupedReporter;

    [CompilerGenerated]
    private int identifiableInitializerSummarizerTotal;

    //
    // Summary:
    //     Gets the currently executing coroutine on this thread, or null, if not executing
    //     in a coroutine.
    public static Coroutine Current => summarizerToken;

    //
    // Summary:
    //     Gets the result returned by the original task.
    //
    // Remarks:
    //     Only valid when Buddy.Coroutines.Coroutine.Status is Buddy.Coroutines.CoroutineStatus.RanToCompletion.
    public object Result { get; private set; }

    //
    // Summary:
    //     Gets the status this coroutine is currently in.
    public CoroutineStatus Status
    {
        [CompilerGenerated]
        get
        {
            return alphabeticRole;
        }
        [CompilerGenerated]
        private set
        {
            alphabeticRole = value;
        }
    }

    //
    // Summary:
    //     Gets a value indicating whether this coroutine has been disposed of.
    public bool IsDisposed
    {
        [CompilerGenerated]
        get
        {
            return isSummarizerTransaction;
        }
        [CompilerGenerated]
        private set
        {
            isSummarizerTransaction = value;
        }
    }

    //
    // Summary:
    //     Gets the exception that was thrown when this coroutine faulted.
    //
    // Remarks:
    //     This can be a Buddy.Coroutines.CoroutineUnhandledException or a Buddy.Coroutines.CoroutineBehaviorException.
    public CoroutineException FaultingException
    {
        [CompilerGenerated]
        get
        {
            return groupedReporter;
        }
        [CompilerGenerated]
        private set
        {
            groupedReporter = value;
        }
    }

    //
    // Summary:
    //     Gets a bool that indicates whether the coroutine is finished
    public bool IsFinished
    {
        get
        {
            if (Status != CoroutineStatus.RanToCompletion && Status != CoroutineStatus.Faulted)
            {
                return Status == CoroutineStatus.Stopped;
            }

            return true;
        }
    }

    //
    // Summary:
    //     Gets the amount of times the coroutine has been resumed/ticked.
    public int Ticks
    {
        [CompilerGenerated]
        get
        {
            return identifiableInitializerSummarizerTotal;
        }
        [CompilerGenerated]
        private set
        {
            identifiableInitializerSummarizerTotal = value;
        }
    }

    private static Func<Task<object>> ReturnsNullWrapper(Func<Task> task)
    {
        _003C_003Ec__DisplayClass3_0 _003C_003Ec__DisplayClass3_ = new _003C_003Ec__DisplayClass3_0();
        _003C_003Ec__DisplayClass3_.engineProxyTransaction = task;
        return async delegate
        {
            await _003C_003Ec__DisplayClass3_.engineProxyTransaction();
            return (object)null;
        };
    }

    //
    // Summary:
    //     Initializes a new Buddy.Coroutines.Coroutine with the specified coroutine task
    //     producer.
    //
    // Parameters:
    //   taskProducer:
    //     A producer that kicks off the coroutine task and returns it.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if taskProducer is null.
    public Coroutine(Func<Task> taskProducer)
    {
        LocalServerSender.ExecuteMixedValue();
        this._002Ector(ReturnsNullWrapper(taskProducer));
    }

    //
    // Summary:
    //     Initializes a new Buddy.Coroutines.Coroutine with the specified coroutine task
    //     producer.
    //
    // Parameters:
    //   taskProducer:
    //     A producer that kicks off the coroutine task and returns it. The result returned
    //     by the task is saved in Buddy.Coroutines.Coroutine.Result when the coroutine
    //     finishes.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if taskProducer is null.
    public Coroutine(Func<Task<object>> taskProducer)
    {
        LocalServerSender.ExecuteMixedValue();
        _VisibleDriverSummarizer = new ModularServiceEngine();
        _003C_003Ec__DisplayClass5_0 CS_0024_003C_003E8__locals0 = new _003C_003Ec__DisplayClass5_0();
        CS_0024_003C_003E8__locals0.m_IdentifiableFilterEngine = taskProducer;
        base._002Ector();
        CS_0024_003C_003E8__locals0._EngineTransactionThread = this;
        if (CS_0024_003C_003E8__locals0.m_IdentifiableFilterEngine == null)
        {
            throw new ArgumentNullException(AnalyzerStrategy.LOUDD90ML(0x3180C9DA ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_ce2d902fa9ac4044bed9da48a69df0e2));
        }

        _ExternalToken = delegate
        {
            Task<object> task;
            try
            {
                task = CS_0024_003C_003E8__locals0.m_IdentifiableFilterEngine();
            }
            catch (Exception innerException)
            {
                throw CS_0024_003C_003E8__locals0._EngineTransactionThread.SetException(new CoroutineUnhandledException(AnalyzerStrategy.LOUDD90ML(0x2634D94E ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_2549d0211e4a4cf8ac702f3a421be6a2), innerException));
            }

            if (task == null)
            {
                throw CS_0024_003C_003E8__locals0._EngineTransactionThread.SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(-54770918 ^ -1613697959 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_8e24f8b4bca241acb0e1a3a906695a09)));
            }

            CS_0024_003C_003E8__locals0._EngineTransactionThread._AnnotationReporter = task;
        };
        Status = CoroutineStatus.Runnable;
    }

    //
    // Summary:
    //     Resumes the coroutine.
    //
    // Exceptions:
    //   T:System.ObjectDisposedException:
    //     Thrown if the coroutine is disposed.
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the coroutine cannot be resumed because it has finished running; see
    //     Buddy.Coroutines.Coroutine.IsFinished. Also thrown if a coroutine tries to resume
    //     itself.
    //
    //   T:Buddy.Coroutines.CoroutineUnhandledException:
    //     Thrown if the coroutine throws an exception. For the exception thrown by the
    //     coroutine, see System.Exception.InnerException.
    //
    //   T:Buddy.Coroutines.CoroutineBehaviorException:
    //     Thrown if the code being executed by this Buddy.Coroutines.Coroutine class behaves
    //     in an unexpected way.
    //
    //     There are several situations where this exception is thrown. They are listed
    //     below.
    //
    //     • The root coroutine task producer (that is, the task producer passed to the
    //     constructor of this class) returns null.
    //     • The coroutine awaits an external task. The coroutine should only await tasks
    //     from the Buddy.Coroutines.Coroutine class, or other tasks that only await tasks
    //     from the Buddy.Coroutines.Coroutine class.
    //     • The coroutine creates multiple tasks without awaiting them. Coroutines should
    //     always immediately await the tasks they create.
    public void Resume()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        if (IsFinished)
        {
            throw new InvalidOperationException(AnalyzerStrategy.LOUDD90ML(0x47D9B3FA ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_c53e3e29baf5402da3e4544bcccef4e0));
        }

        if (summarizerToken == this)
        {
            throw new InvalidOperationException(AnalyzerStrategy.LOUDD90ML(0x1BDD9156 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_be749d8d5ac3433fbfd0fe5f4b35662e));
        }

        Resume(forStop: false);
    }

    private void Resume(bool forStop)
    {
        SynchronizationContext current = SynchronizationContext.Current;
        Coroutine coroutine = summarizerToken;
        try
        {
            summarizerToken = this;
            SynchronizationContext.SetSynchronizationContext(null);
            Action externalToken = _ExternalToken;
            _ExternalToken = null;
            externalToken();
            if (!forStop)
            {
                Ticks++;
            }

            CheckPostConditions(forStop);
        }
        finally
        {
            summarizerToken = coroutine;
            SynchronizationContext.SetSynchronizationContext(current);
        }
    }

    private Exception SetException(CoroutineException ex)
    {
        Status = CoroutineStatus.Faulted;
        FaultingException = ex;
        return ex;
    }

    private void CheckPostConditions(bool shouldBeCanceled)
    {
        switch (_AnnotationReporter.Status)
        {
            case TaskStatus.RanToCompletion:
                if (_ExternalToken != null)
                {
                    throw SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(0x47D9B23A ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_c53e3e29baf5402da3e4544bcccef4e0)));
                }

                Status = CoroutineStatus.RanToCompletion;
                Result = _AnnotationReporter.Result;
                break;
            case TaskStatus.WaitingForActivation:
                if (shouldBeCanceled)
                {
                    throw SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(0x2BB62F62 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_de714349c8a0449db5bc4e0866925e17)));
                }

                if (_ExternalToken == null)
                {
                    throw SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(0x660CB90 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_51b14c7dbce74598ac76e758099898dc)));
                }

                break;
            case TaskStatus.Faulted:
                {
                    Exception ex = Enumerable.FirstOrDefault(_AnnotationReporter.Exception.InnerExceptions);
                    if (ex is CoroutineStoppedException)
                    {
                        Status = CoroutineStatus.Stopped;
                        break;
                    }

                    throw SetException(new CoroutineUnhandledException(AnalyzerStrategy.LOUDD90ML(0x534ACFD4 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_7500dbcc6c394ed5bbe96a60b6dc435f), ex));
                }
            case TaskStatus.Canceled:
                try
                {
                    _AnnotationReporter.WaitOrUnwrap(0);
                    throw SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(0x3E5E9DA0 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_2529a54d52064d849ec4e6774128a0e7)));
                }
                catch (Exception innerException)
                {
                    throw SetException(new CoroutineUnhandledException(AnalyzerStrategy.LOUDD90ML(0x4620ACE5 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_258343011ca94c558df40384e5f63ca9), innerException));
                }
            default:
                throw SetException(new CoroutineBehaviorException(AnalyzerStrategy.LOUDD90ML(0x6BF312D4 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_5d4a852e51f14e0e95994759832bd377) + _AnnotationReporter.Status));
        }
    }

    //
    // Summary:
    //     Disposes this coroutine.
    //
    // Exceptions:
    //   T:Buddy.Coroutines.CoroutineBehaviorException:
    //     Thrown if the coroutine being disposed of catches the Buddy.Coroutines.CoroutineStoppedException
    //     thrown.
    //
    //   T:Buddy.Coroutines.CoroutineStoppedException:
    //     Thrown if the coroutine being disposed of is the current coroutine. This exception
    //     is expected and handled by the coroutine framework, and should therefore not
    //     be caught.
    //
    // Remarks:
    //     Disposing a coroutine before it has finished running is a complicated process
    //     which requires unwinding the coroutine's tasks to make sure any finally blocks
    //     are executed.
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (Current == this)
        {
            IsDisposed = true;
            throw CanceledException();
        }

        if (Status == CoroutineStatus.Runnable)
        {
            if (_AnnotationReporter != null)
            {
                _VisibleDriverSummarizer.SpecifyStaticMap(cantask: true);
                Resume(forStop: true);
            }
            else
            {
                _ExternalToken = null;
            }
        }

        IsDisposed = true;
    }

    //
    // Summary:
    //     Gets a string representation of this coroutine.
    public override string ToString()
    {
        switch (Status)
        {
            case CoroutineStatus.Faulted:
                if (FaultingException != null)
                {
                    return AnalyzerStrategy.LOUDD90ML(0x188FF849 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_3c1c79ed2c6c42c3a00180eb3a40bac4) + FaultingException.InnerException.GetType();
                }

                return AnalyzerStrategy.LOUDD90ML(0x213D49DE ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_b663181093b7446db7d0761c5651db35);
            case CoroutineStatus.RanToCompletion:
                if (Result != null)
                {
                    return AnalyzerStrategy.LOUDD90ML(--566657269 ^ 0x43942999 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_6bea25356f0e4c6980ccc02220528d9a) + Result;
                }

                return AnalyzerStrategy.LOUDD90ML(0x3E5EA2AE ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_2529a54d52064d849ec4e6774128a0e7);
            case CoroutineStatus.Runnable:
                return AnalyzerStrategy.LOUDD90ML(0x5E1CFC06 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_4462ed0a803c4ef6947dda77ad90cbef);
            case CoroutineStatus.Stopped:
                return AnalyzerStrategy.LOUDD90ML(0x3C6FD929 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_c042bb0cd603462998d4389eaffa04a3);
            default:
                return AnalyzerStrategy.LOUDD90ML(0x503151EE ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_dba187cb09c0457989b39128cc1f691c);
        }
    }

    internal static Exception CanceledException()
    {
        return new CoroutineStoppedException(AnalyzerStrategy.LOUDD90ML(0x2245B128 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_32b10334235841f79862e993a7feb3ab));
    }

    //
    // Summary:
    //     Ensures the current function is executing in a coroutine and raises an exception
    //     if not.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    private static void CheckObtainCoroutineTask()
    {
        if ((summarizerToken ?? throw new InvalidOperationException(AnalyzerStrategy.LOUDD90ML(0x55CD7304 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_559254b8f0034d7e9c5fe154059c25fd)))._ExternalToken != null)
        {
            throw new InvalidOperationException(AnalyzerStrategy.LOUDD90ML(0x47D98852 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_c53e3e29baf5402da3e4544bcccef4e0));
        }
    }

    private static async Task InternalYield()
    {
        ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
        if (!modularServiceEngine.IsCompleted)
        {
            await modularServiceEngine;
            object obj = default(object);
            modularServiceEngine = (ModularServiceEngine)obj;
        }

        modularServiceEngine.GetResult();
    }

    //
    // Summary:
    //     Yields back to the coroutine, executing the rest of the current function in the
    //     next tick.
    //
    // Returns:
    //     The coroutine task.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    public static Task Yield()
    {
        CheckObtainCoroutineTask();
        return InternalYield();
    }

    private static async Task InternalSleep(TimeSpan timeout)
    {
        object obj = default(object);
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            while (true)
            {
                ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
                if (!modularServiceEngine.IsCompleted)
                {
                    await modularServiceEngine;
                    modularServiceEngine = (ModularServiceEngine)obj;
                    obj = null;
                }

                modularServiceEngine.GetResult();
            }
        }

        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
            if (!modularServiceEngine.IsCompleted)
            {
                await modularServiceEngine;
                modularServiceEngine = (ModularServiceEngine)obj;
                obj = null;
            }

            modularServiceEngine.GetResult();
        }
        while (timer.Elapsed < timeout);
    }

    //
    // Summary:
    //     Gets a coroutine task that sleeps for the specified timeout.
    //
    // Parameters:
    //   timeout:
    //     The timeout to sleep.
    //
    // Returns:
    //     The coroutine task.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown if timeout is negative and not equal to System.Threading.Timeout.InfiniteTimeSpan.
    public static Task Sleep(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x75FE5745 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_f9f84da3661f4e4d85925f714b662ef6));
        }

        CheckObtainCoroutineTask();
        return InternalSleep(timeout);
    }

    //
    // Summary:
    //     Gets a coroutine task that sleeps for the specified amount of milliseconds.
    //
    // Parameters:
    //   milliseconds:
    //     The amount of milliseconds to sleep.
    //
    // Returns:
    //     The coroutine task.
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown if milliseconds is negative and not equal to System.Threading.Timeout.Infinite.
    public static Task Sleep(int milliseconds)
    {
        if (milliseconds < 0 && milliseconds != -1)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x6BE3D9BD ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_7eec1bd50a934476accc918014ebe811));
        }

        return Sleep(new TimeSpan(0, 0, 0, 0, milliseconds));
    }

    private static async Task<bool> InternalWait(TimeSpan maxTimeout, Func<bool> condition)
    {
        if (maxTimeout == TimeSpan.Zero)
        {
            return condition();
        }

        object obj = default(object);
        if (maxTimeout == Timeout.InfiniteTimeSpan)
        {
            while (!condition())
            {
                ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
                if (!modularServiceEngine.IsCompleted)
                {
                    await modularServiceEngine;
                    modularServiceEngine = (ModularServiceEngine)obj;
                    obj = null;
                }

                modularServiceEngine.GetResult();
            }

            return true;
        }

        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            if (condition())
            {
                return true;
            }

            ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
            if (!modularServiceEngine.IsCompleted)
            {
                await modularServiceEngine;
                modularServiceEngine = (ModularServiceEngine)obj;
                obj = null;
            }

            modularServiceEngine.GetResult();
        }
        while (timer.Elapsed < maxTimeout);
        return false;
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for the specified condition to become true,
    //     for up to the specified max time. Returns true if the condition becomes true
    //     before the max wait time is over.
    //
    // Parameters:
    //   maxWaitTimeout:
    //     The max time to wait, or Timeout.InfiniteTimeSpan for an infinite wait.
    //
    //   condition:
    //     The condition.
    //
    // Returns:
    //     A coroutine task that returns true if condition evaluates to true during the
    //     timeout period; otherwise false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown when condition is null.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown when maxWaitTimeout is negative and not equal to System.Threading.Timeout.InfiniteTimeSpan.
    //
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    public static Task<bool> Wait(TimeSpan maxWaitTimeout, Func<bool> condition)
    {
        if (maxWaitTimeout < TimeSpan.Zero && maxWaitTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x46209031 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_258343011ca94c558df40384e5f63ca9), AnalyzerStrategy.LOUDD90ML(0x60A5E348 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_3ef5c5cb24ce445c9d65725277c691c1));
        }

        if (condition == null)
        {
            throw new ArgumentNullException(AnalyzerStrategy.LOUDD90ML(0x40DBE2DA ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_50d8a2c630fb4265bc0b6394f7e89394));
        }

        CheckObtainCoroutineTask();
        return InternalWait(maxWaitTimeout, condition);
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for the specified condition to become true,
    //     for up to the specified max time. Returns true if the condition becomes true
    //     before the max wait time is over.
    //
    // Parameters:
    //   maxWaitMs:
    //     The max time to wait, in milliseconds, or Timeout.Infinite for an infinite wait.
    //
    //
    //   condition:
    //     The condition.
    //
    // Returns:
    //     A coroutine task that returns true if condition evaluates to true during the
    //     timeout period; otherwise false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown when condition is null.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown when maxWaitMs is negative and not equal to System.Threading.Timeout.Infinite.
    //
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    public static Task<bool> Wait(int maxWaitMs, Func<bool> condition)
    {
        if (maxWaitMs < 0 && maxWaitMs != -1)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x60A5E31E ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_3ef5c5cb24ce445c9d65725277c691c1), AnalyzerStrategy.LOUDD90ML(0x660F404 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_51b14c7dbce74598ac76e758099898dc));
        }

        return Wait(new TimeSpan(0, 0, 0, 0, maxWaitMs), condition);
    }

    private static async Task<bool> InternalWaitForExternalTask(Task externalTask, TimeSpan timeout)
    {
        if (timeout == TimeSpan.Zero)
        {
            return externalTask.WaitOrUnwrap(0);
        }

        object obj = default(object);
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            while (!externalTask.WaitOrUnwrap(0))
            {
                ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
                if (!modularServiceEngine.IsCompleted)
                {
                    await modularServiceEngine;
                    modularServiceEngine = (ModularServiceEngine)obj;
                    obj = null;
                }

                modularServiceEngine.GetResult();
            }

            return true;
        }

        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            if (externalTask.WaitOrUnwrap(0))
            {
                return true;
            }

            ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
            if (!modularServiceEngine.IsCompleted)
            {
                await modularServiceEngine;
                modularServiceEngine = (ModularServiceEngine)obj;
                obj = null;
            }

            modularServiceEngine.GetResult();
        }
        while (timer.Elapsed < timeout);
        return false;
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for completion of an external task (a task not
    //     running as a coroutine).
    //
    // Parameters:
    //   externalTask:
    //     The external task.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if externalTask is null.
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    // Remarks:
    //     Do not pass a coroutine task to this function. Doing so will result in possible
    //     dead locks and exceptions.
    public static Task ExternalTask(Task externalTask)
    {
        return ExternalTask(externalTask, Timeout.InfiniteTimeSpan);
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for completion of an external task (a task not
    //     running as a coroutine).
    //
    // Parameters:
    //   externalTask:
    //     The external task.
    //
    //   timeout:
    //     The max time to wait for the external task to complete.
    //
    // Returns:
    //     true if the external task completed within timeout; otherwise false.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if externalTask is null.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown if timeout is negative (and not equal to System.Threading.Timeout.InfiniteTimeSpan).
    //
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    // Remarks:
    //     Do not pass a coroutine task to this function. Doing so will result in possible
    //     dead locks and exceptions.
    public static Task<bool> ExternalTask(Task externalTask, TimeSpan timeout)
    {
        if (externalTask == null)
        {
            throw new ArgumentNullException(AnalyzerStrategy.LOUDD90ML(0x6345EE58 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_1016568081a04c1eb8a1e1743b6a1e6d));
        }

        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x625294F8 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_6bea25356f0e4c6980ccc02220528d9a));
        }

        CheckObtainCoroutineTask();
        return InternalWaitForExternalTask(externalTask, timeout);
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for completion of an external task (a task not
    //     running as a coroutine).
    //
    // Parameters:
    //   externalTask:
    //     The external task.
    //
    //   millisecondsTimeout:
    //     The max time to wait for the external task to complete, in milliseconds.
    //
    // Returns:
    //     true if the external task completed within millisecondsTimeout; otherwise false.
    //
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if externalTask is null.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown if millisecondsTimeout is negative (and not equal to System.Threading.Timeout.Infinite).
    //
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    // Remarks:
    //     Do not pass a coroutine task to this function. Doing so will result in possible
    //     dead locks and exceptions.
    public static Task<bool> ExternalTask(Task externalTask, int millisecondsTimeout)
    {
        if (millisecondsTimeout != -1 && millisecondsTimeout < 0)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x503152D4 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_dba187cb09c0457989b39128cc1f691c), AnalyzerStrategy.LOUDD90ML(0x3E5EA12A ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_2529a54d52064d849ec4e6774128a0e7));
        }

        return ExternalTask(externalTask, new TimeSpan(0, 0, 0, 0, millisecondsTimeout));
    }

    private static async Task<ExternalTaskWaitResult<T>> InternalWaitForExternalTask<T>(Task<T> externalTask, TimeSpan timeout)
    {
        if (timeout == TimeSpan.Zero)
        {
            return externalTask.WaitOrUnwrap(0) ? ExternalTaskWaitResult<T>.WithResult(externalTask.Result) : ExternalTaskWaitResult<T>.activeResolverSummarizer;
        }

        object obj = default(object);
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            while (!externalTask.WaitOrUnwrap(0))
            {
                ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
                if (!modularServiceEngine.IsCompleted)
                {
                    await modularServiceEngine;
                    modularServiceEngine = (ModularServiceEngine)obj;
                    obj = null;
                }

                modularServiceEngine.GetResult();
            }

            return ExternalTaskWaitResult<T>.WithResult(externalTask.Result);
        }

        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            if (externalTask.WaitOrUnwrap(0))
            {
                return ExternalTaskWaitResult<T>.WithResult(externalTask.Result);
            }

            ModularServiceEngine modularServiceEngine = Current._VisibleDriverSummarizer.GetAwaiter();
            if (!modularServiceEngine.IsCompleted)
            {
                await modularServiceEngine;
                modularServiceEngine = (ModularServiceEngine)obj;
                obj = null;
            }

            modularServiceEngine.GetResult();
        }
        while (timer.Elapsed < timeout);
        return ExternalTaskWaitResult<T>.activeResolverSummarizer;
    }

    private static async Task<T> ExtractWaitResult<T>(Task<ExternalTaskWaitResult<T>> task)
    {
        return (await task).Result;
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for completion of an external task (a task not
    //     running as a coroutine).
    //
    // Parameters:
    //   externalTask:
    //     The external task.
    //
    // Type parameters:
    //   T:
    //     The return type of the external task.
    //
    // Returns:
    //     The result of the external task.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if externalTask is null.
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    // Remarks:
    //     Do not pass a coroutine task to this function. Doing so will result in possible
    //     dead locks and exceptions.
    public static Task<T> ExternalTask<T>(Task<T> externalTask)
    {
        return ExtractWaitResult(ExternalTask(externalTask, Timeout.InfiniteTimeSpan));
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for completion of an external task (a task not
    //     running as a coroutine).
    //
    // Parameters:
    //   externalTask:
    //     The external task.
    //
    //   timeout:
    //     The max time to wait for the external task to complete.
    //
    // Type parameters:
    //   T:
    //     The return type of the external task.
    //
    // Returns:
    //     The result of the wait which indicates whether the external task timed out or
    //     not, and if not, the actual result.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if externalTask is null.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown if timeout is negative (and not equal to System.Threading.Timeout.InfiniteTimeSpan).
    //
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    // Remarks:
    //     Do not pass a coroutine task to this function. Doing so will result in possible
    //     dead locks and exceptions.
    public static Task<ExternalTaskWaitResult<T>> ExternalTask<T>(Task<T> externalTask, TimeSpan timeout)
    {
        if (externalTask == null)
        {
            throw new ArgumentNullException(AnalyzerStrategy.LOUDD90ML(0x9D6BA3A ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_8b917ebefcd945339f58c82882581294));
        }

        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x3180F138 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_ce2d902fa9ac4044bed9da48a69df0e2), AnalyzerStrategy.LOUDD90ML(0x6BF32E00 ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_5d4a852e51f14e0e95994759832bd377));
        }

        CheckObtainCoroutineTask();
        return InternalWaitForExternalTask(externalTask, timeout);
    }

    //
    // Summary:
    //     Gets a coroutine task that waits for completion of an external task (a task not
    //     running as a coroutine).
    //
    // Parameters:
    //   externalTask:
    //     The external task.
    //
    //   millisecondsTimeout:
    //     The max time to wait for the external task to complete, in milliseconds.
    //
    // Type parameters:
    //   T:
    //     The return type of the external task.
    //
    // Returns:
    //     The result of the wait which indicates whether the external task timed out or
    //     not, and if not, the actual result.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     Thrown if externalTask is null.
    //
    //   T:System.ArgumentOutOfRangeException:
    //     Thrown if millisecondsTimeout is negative (and not equal to System.Threading.Timeout.Infinite).
    //
    //
    //   T:System.InvalidOperationException:
    //     Thrown if the function is not executing in a coroutine, or if it has already
    //     obtained a coroutine task this tick.
    //
    // Remarks:
    //     Do not pass a coroutine task to this function. Doing so will result in possible
    //     dead locks and exceptions.
    public static Task<ExternalTaskWaitResult<T>> ExternalTask<T>(Task<T> externalTask, int millisecondsTimeout)
    {
        if (millisecondsTimeout < 0 && millisecondsTimeout != -1)
        {
            throw new ArgumentOutOfRangeException(AnalyzerStrategy.LOUDD90ML(0x20E96DAA ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_b3fbc5b7fe4f4f8daafa12602fc36cc5), AnalyzerStrategy.LOUDD90ML(--673209962 ^ 0x2F406FAF ^ _003CModule_003E_007B68b90f86_002D1d5c_002D4f47_002Db742_002D4239677df5b6_007D.m_ae86919212914e66b596e43bc4a3c377.m_c73f98d5abd24d81a92b408342534699));
        }

        return ExternalTask(externalTask, new TimeSpan(0, 0, 0, 0, millisecondsTimeout));
    }

    static Coroutine()
    {
        AnalyzerStrategy.uc8lngiwB();
    }
}
#if false // Decompilation log
'114' items in cache
------------------
Resolve: 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.dll'
------------------
Resolve: 'PresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'PresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll'
------------------
Resolve: 'PresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'PresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
WARN: Version mismatch. Expected: '2.1.0.0', Got: '2.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\netstandard.dll'
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.dll'
------------------
Resolve: 'System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.dll'
------------------
Resolve: 'mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e, Retargetable=true'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '2.0.5.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ObjectModel.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Text.RegularExpressions.dll'
------------------
Resolve: 'System.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Net.WebClient, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Net.WebClient, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Windows.Forms, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Windows.Forms, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Configuration.ConfigurationManager, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Configuration.ConfigurationManager, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.dll'
------------------
Resolve: 'WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Net.Http, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.2.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Net.Http.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.dll'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Reflection.Metadata, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Reflection.Metadata, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Diagnostics.Tracing, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Diagnostics.Tracing, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Runtime.Loader, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Runtime.Loader, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Xml.XDocument, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.XDocument, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.XDocument.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Linq.Expressions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.Expressions.dll'
------------------
Resolve: 'System.Runtime.Serialization.Formatters, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Formatters, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Formatters.dll'
------------------
Resolve: 'System.ObjectModel, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.2.2.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.Serialization.Xml, Version=4.1.5.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Xml, Version=4.1.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.5.0', Got: '4.1.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Xml.dll'
------------------
Resolve: 'Grpc.Core.Api, Version=2.0.0.0, Culture=neutral, PublicKeyToken=d754f35622e28bad'
Could not find by name: 'Grpc.Core.Api, Version=2.0.0.0, Culture=neutral, PublicKeyToken=d754f35622e28bad'
------------------
Resolve: 'System, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e, Retargetable=true'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '2.0.5.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll'
------------------
Resolve: 'System.Diagnostics.Tracing, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Diagnostics.Tracing, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Security.Claims, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Claims, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Security.Claims.dll'
------------------
Resolve: 'System.Runtime.Serialization.Xml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Xml, Version=4.1.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Xml.dll'
------------------
Resolve: 'System.Threading, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.Specialized.dll'
------------------
Resolve: 'System.ComponentModel.EventBasedAsync, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.EventBasedAsync, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.EventBasedAsync.dll'
------------------
Resolve: 'System.Xml.ReaderWriter, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.ReaderWriter, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.ReaderWriter.dll'
------------------
Resolve: 'System.Reflection.DispatchProxy, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Reflection.DispatchProxy, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'GreyMagic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=260525fa2b0e778a'
Could not find by name: 'GreyMagic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=260525fa2b0e778a'
------------------
Resolve: 'System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.dll'
------------------
Resolve: 'System.Linq.Expressions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.Expressions.dll'
------------------
Resolve: 'System.Threading.Thread, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.Thread.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Runtime.Serialization.Formatters, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Formatters, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Formatters.dll'
------------------
Resolve: 'System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.Encoding.Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.Encoding.Extensions, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Text.Encoding.Extensions.dll'
------------------
Resolve: 'LibGit2Sharp, Version=0.31.0.0, Culture=neutral, PublicKeyToken=7cbde695407f0333'
Could not find by name: 'LibGit2Sharp, Version=0.31.0.0, Culture=neutral, PublicKeyToken=7cbde695407f0333'
------------------
Resolve: 'Clio.Localization, Version=1.0.802.0, Culture=neutral, PublicKeyToken=48d7174f8a943034'
Could not find by name: 'Clio.Localization, Version=1.0.802.0, Culture=neutral, PublicKeyToken=48d7174f8a943034'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Security.Cryptography.ProtectedData, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Security.Cryptography.ProtectedData, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.Concurrent.dll'
------------------
Resolve: 'System.Threading.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Threading.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Security.Principal.Windows, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Security.Principal.Windows, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Security.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Security.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Console, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Console.dll'
------------------
Resolve: 'Microsoft.CSharp, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Microsoft.CSharp.dll'
------------------
Resolve: 'System.Net.ServicePoint, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Net.ServicePoint, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Runtime.CompilerServices.VisualC, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.CompilerServices.VisualC, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.CompilerServices.VisualC.dll'
------------------
Resolve: 'System.Threading.Tasks.Parallel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Tasks.Parallel, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.Tasks.Parallel.dll'
------------------
Resolve: 'System.Net.WebHeaderCollection, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebHeaderCollection, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Net.WebHeaderCollection.dll'
------------------
Resolve: 'System.Linq.Parallel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Parallel, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.Parallel.dll'
------------------
Resolve: 'System.ComponentModel.Annotations, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Annotations, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.10.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.Annotations.dll'
------------------
Resolve: 'System.Net.Ping, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Ping, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Net.Ping.dll'
------------------
Resolve: 'System.Drawing.Common, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Drawing.Common, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'SlimDX, Version=4.0.13.43, Culture=neutral, PublicKeyToken=ad9e8c0370b029db'
Could not find by name: 'SlimDX, Version=4.0.13.43, Culture=neutral, PublicKeyToken=ad9e8c0370b029db'
------------------
Resolve: 'System.Windows.Forms.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Windows.Forms.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Drawing.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.Web.HttpUtility, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Web.HttpUtility, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.IO.Compression.ZipFile, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression.ZipFile, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.IO.Compression.ZipFile.dll'
------------------
Resolve: 'System.IO.Compression, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.IO.Compression, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Xml.XDocument, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.XDocument, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.XDocument.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Net.Requests, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Requests, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Net.Requests.dll'
------------------
Resolve: 'System.Xml.XmlSerializer, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.XmlSerializer, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.XmlSerializer.dll'
------------------
Resolve: 'Grpc.Net.Client, Version=2.0.0.0, Culture=neutral, PublicKeyToken=d754f35622e28bad'
Could not find by name: 'Grpc.Net.Client, Version=2.0.0.0, Culture=neutral, PublicKeyToken=d754f35622e28bad'
------------------
Resolve: 'System.Net.NameResolution, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Reflection.Emit.Lightweight, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.Lightweight, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.Lightweight.dll'
------------------
Resolve: 'System.Reflection.Emit.ILGeneration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.ILGeneration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.ILGeneration.dll'
------------------
Resolve: 'System.Reflection.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Primitives, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Primitives.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Security.Cryptography, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Security.Cryptography, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Core.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll'
------------------
Resolve: 'System.IO.FileSystem.Watcher, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.FileSystem.Watcher, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.IO.FileSystem.Watcher.dll'
------------------
Resolve: 'Microsoft.Scripting, Version=1.3.1.0, Culture=neutral, PublicKeyToken=7f709c5b713576e1'
Could not find by name: 'Microsoft.Scripting, Version=1.3.1.0, Culture=neutral, PublicKeyToken=7f709c5b713576e1'
------------------
Resolve: 'IronPython, Version=2.7.12.0, Culture=neutral, PublicKeyToken=7f709c5b713576e1'
Could not find by name: 'IronPython, Version=2.7.12.0, Culture=neutral, PublicKeyToken=7f709c5b713576e1'
------------------
Resolve: 'System.CodeDom, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.CodeDom, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Collections.NonGeneric, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Net.Quic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Net.Quic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Text.Encoding.CodePages, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Text.Encoding.CodePages, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Resources.Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Resources.Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.dll'
------------------
Resolve: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.Expressions.dll'
------------------
Resolve: 'System.Memory, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Memory, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Console.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Threading.Tasks.Parallel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Tasks.Parallel, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.Tasks.Parallel.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.Concurrent.dll'
------------------
Resolve: 'System.Text.Encoding.Extensions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.Encoding.Extensions, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Text.Encoding.Extensions.dll'
------------------
Resolve: 'System.Xml.ReaderWriter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.ReaderWriter, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.ReaderWriter.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Text.RegularExpressions.dll'
------------------
Resolve: 'System.Runtime.Intrinsics, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Runtime.Intrinsics, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=4.0.12.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.12.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Xml.XPath.XDocument, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.XPath.XDocument, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.1.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.XPath.XDocument.dll'
------------------
Resolve: 'System.Reflection.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Primitives, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Primitives.dll'
------------------
Resolve: 'System.Runtime.Numerics, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Numerics, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Numerics.dll'
------------------
Resolve: 'System.Text.Encoding.CodePages, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Text.Encoding.CodePages, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Threading, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.dll'
------------------
Resolve: 'System.Runtime.Numerics, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Numerics, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Numerics.dll'
------------------
Resolve: 'System.Xml.ReaderWriter, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.ReaderWriter, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.ReaderWriter.dll'
------------------
Resolve: 'System.Xml.XDocument, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.XDocument, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.XDocument.dll'
------------------
Resolve: 'System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.dll'
------------------
Resolve: 'System.Linq, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Text.RegularExpressions.dll'
------------------
Resolve: 'System.Reflection.Emit.Lightweight, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.Lightweight, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.Lightweight.dll'
------------------
Resolve: 'System.Reflection.Emit.ILGeneration, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.ILGeneration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.ILGeneration.dll'
------------------
Resolve: 'System.Reflection.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Primitives, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Primitives.dll'
------------------
Resolve: 'System.Runtime.Serialization.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Primitives, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.2.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Primitives.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.Concurrent.dll'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Data.Common, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.2.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Data.Common.dll'
------------------
Resolve: 'System.Text.Encoding.Extensions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.Encoding.Extensions, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '6.0.0.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Text.Encoding.Extensions.dll'
------------------
Resolve: 'System.Runtime.Extensions, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Extensions, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.2.2.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Extensions.dll'
------------------
Resolve: 'System.Threading, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.dll'
------------------
Resolve: 'System.Linq, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.2.2.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.dll'
------------------
Resolve: 'System.Collections, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Collections.dll'
------------------
Resolve: 'System.Xml.ReaderWriter, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Xml.ReaderWriter, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.2.2.0', Got: '4.1.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Xml.ReaderWriter.dll'
------------------
Resolve: 'System.Reflection.Emit, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.dll'
------------------
Resolve: 'System.Reflection.Emit.Lightweight, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.Lightweight, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.1.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.Lightweight.dll'
------------------
Resolve: 'System.Reflection.Emit.ILGeneration, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.ILGeneration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.1.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.ILGeneration.dll'
------------------
Resolve: 'System.Reflection.Primitives, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Primitives, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Primitives.dll'
------------------
Resolve: 'System.IO.FileSystem, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.FileSystem, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.3.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.IO.FileSystem.dll'
------------------
Resolve: 'System.Threading.Channels, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Threading.Channels, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Reflection.Emit, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Reflection.Emit.dll'
------------------
Resolve: 'System.IO.Pipelines, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.IO.Pipelines, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Threading.ThreadPool, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=4.0.12.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.12.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Core, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e, Retargetable=true'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '2.0.5.0', Got: '4.0.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Core.dll'
------------------
Resolve: 'System.Threading.Overlapped, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Overlapped, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.1.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Threading.Overlapped.dll'
------------------
Resolve: 'System.Net.Security, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Security, Version=4.0.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Net.Security.dll'
------------------
Resolve: 'System.Security.Cryptography.Xml, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Security.Cryptography.Xml, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Linq.Queryable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Queryable, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Linq.Queryable.dll'
------------------
Resolve: 'System.Runtime.Serialization.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Primitives, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '8.0.0.0', Got: '4.2.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Primitives.dll'
------------------
Resolve: 'System.Runtime.Serialization.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Primitives, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '7.0.0.0', Got: '4.2.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.Serialization.Primitives.dll'
------------------
Resolve: 'System.Diagnostics.Tools, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Tools, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.1.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.Tools.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.2.2.0', Got: '4.1.2.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Diagnostics.Debug, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Debug, Version=4.0.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.1.2.0', Got: '4.0.11.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Diagnostics.Debug.dll'
------------------
Resolve: 'System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Data.dll'
------------------
Resolve: 'System.Diagnostics.Tracing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Diagnostics.Tracing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '4.0.0.0', Got: '4.2.0.0'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Net.Http.dll'
------------------
Resolve: 'System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.dll'
------------------
Resolve: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.Linq.dll'
------------------
Resolve: 'System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=8.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'System.Runtime.CompilerServices.Unsafe, Version=8.0.0.0, Culture=neutral, PublicKeyToken=null'
#endif
