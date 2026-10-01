using System;
using System.Collections.Generic;
using UnityEngine;

namespace SleepyStudios.LoopScroll.Internal
{
    internal sealed class ListDataSource<TItem, TCell> : ILoopDataSource where TCell : Component
    {
        private readonly IReadOnlyList<TItem> items;
        private readonly Action<TCell, TItem, CellBindContext> bind;
        private readonly Action<TCell, CellBindContext> unbind;
        private readonly Func<TItem, string> keys;
        private readonly float estimate;
        public int Count => items.Count;
        public bool HasStableKeys => keys != null;
        public ListDataSource(IReadOnlyList<TItem> items, Action<TCell, TItem, CellBindContext> bind,
            Action<TCell, CellBindContext> unbind, Func<TItem, string> keys, float estimate)
        { this.items = items; this.bind = bind; this.unbind = unbind; this.keys = keys; this.estimate = estimate; }
        public string GetItemKey(int index) => keys != null ? keys(items[index]) : index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public int GetCellType(int index) => 0;
        public float GetEstimatedSize(int index, float crossAxisSize) => estimate;
        public void BindCell(LoopCell cell, int index, CellBindContext context)
        {
            var component = cell.GetComponent<TCell>();
            if (component == null) throw new InvalidOperationException($"Cell Prefab 缺少 {typeof(TCell).FullName}");
            bind(component, items[index], context);
        }
        public void UnbindCell(LoopCell cell, CellBindContext context) => unbind?.Invoke(cell.GetComponent<TCell>(), context);
    }
}
