using System.Threading;

namespace SleepyStudios.LoopScroll
{
    public readonly struct CellBindContext
    {
        private readonly LoopCell owner;
        public readonly string Key;
        public readonly long Version;
        public readonly int Index;
        internal CellBindContext(LoopCell cell, string key, long version, int index)
        { owner = cell; Key = key; Version = version; Index = index; }
        /// 当前 Cell 仍属于本次绑定；异步写入前检查此属性。
        public bool IsCurrent => owner != null && owner.IsCurrent(Version);
        /// 首次获取时才创建 CTS；已失效上下文始终返回取消 Token。
        public CancellationToken CancellationToken => owner != null ? owner.GetToken(Version) : new CancellationToken(true);
    }
}
