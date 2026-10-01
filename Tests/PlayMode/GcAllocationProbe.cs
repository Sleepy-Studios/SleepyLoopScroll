using System;
using NUnit.Framework;
using Unity.Profiling;

namespace SleepyStudios.LoopScroll.Tests
{
    internal sealed class GcAllocationProbe : IDisposable
    {
        private ProfilerRecorder recorder;
        public GcAllocationProbe()
        {
            recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 4096, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            // 原生 recorder 必须能检测已知分配，避免 Mono 的零值 API 造成假阳性。
            GC.KeepAlive(new byte[1024]); recorder.Stop();
            Assert.That(recorder.Valid, Is.True);
            Assert.That(recorder.Count, Is.GreaterThan(0), "GC.Alloc recorder 未记录校准分配，不能据此声称零 GC。");
        }
        public void Begin() { recorder.Reset(); recorder.Start(); }
        public long End()
        {
            recorder.Stop(); var bytes = 0L;
            for (var i = 0; i < recorder.Count; i++) bytes += recorder.GetSample(i).Value;
            return bytes;
        }
        public void Dispose() { recorder.Dispose(); }
    }
}
