using System.Runtime.CompilerServices;

namespace Lattice.Core.Tests;

internal static class TestHostThreadPoolInitializer
{
    [ModuleInitializer]
    internal static void EnsureMinimumWorkerThreadsForBlockingTests()
    {
        ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
        int targetWorkers = Math.Max(workerThreads, 16);
        int targetIo = Math.Max(completionPortThreads, 16);
        ThreadPool.SetMinThreads(targetWorkers, targetIo);
    }
}
