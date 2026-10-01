using System;
using System.Collections;

namespace SleepyStudios.LoopScroll.Internal
{
    // 每次提交独立保存委托，后续重新注册不会改变旧 Cell 的解绑契约。
    internal sealed class ListDataSource : ILoopDataSource
    {
        private readonly IList items;
        private readonly Action<LoopCell, int, CellBindContext> bind;
        private readonly Action<LoopCell, CellBindContext> unbind;
        private readonly Func<object, string> keys;
        private readonly float estimate;
        public ListDataSource(IList items, Action<LoopCell, int, CellBindContext> bind,
            Action<LoopCell, CellBindContext> unbind, Func<object, string> keys, float estimate)
        { this.items = items; this.bind = bind; this.unbind = unbind; this.keys = keys; this.estimate = estimate; }
        public int Count => items.Count;
        public bool HasStableKeys => keys != null;
        public string GetItemKey(int index) => keys != null ? keys(items[index]) : index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public int GetCellType(int index) => 0;
        public float GetEstimatedSize(int index, float crossAxisSize) => estimate;
        public void BindCell(LoopCell cell, int index, CellBindContext context) => bind(cell, index, context);
        public void UnbindCell(LoopCell cell, CellBindContext context) => unbind?.Invoke(cell, context);
    }
}
