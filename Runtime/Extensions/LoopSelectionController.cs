using System;
using System.Collections.Generic;
using UnityEngine;

namespace SleepyStudios.LoopScroll
{
    [DisallowMultipleComponent, RequireComponent(typeof(LoopScrollView))]
    public sealed class LoopSelectionController : MonoBehaviour
    {
        [SerializeField] private bool multiSelect;
        private LoopScrollView list;
        private readonly HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> removed = new List<string>();
        private Action<LoopCell, CellBindContext> updateCell;
        public IReadOnlyCollection<string> SelectedKeys => selected;
        public event Action SelectionChanged;
        public event Action<LoopCell, bool> CellSelectionChanged;
        public bool MultiSelect { get => multiSelect; set { multiSelect = value; if (!value && selected.Count > 1) ClearSelection(); } }
        private void Awake() { list = GetComponent<LoopScrollView>(); updateCell = OnBound; }
        private void OnEnable() { list.CellClicked += OnClick; list.CellBound += OnBound; list.DataChanged += OnData; OnData(); }
        private void OnDisable() { list.CellClicked -= OnClick; list.CellBound -= OnBound; list.DataChanged -= OnData; }
        private void OnClick(LoopCell cell, CellBindContext context)
        { if (context.IsCurrent && list.TryGetIndex(context.Key, out var index)) SetSelected(context.Key, !selected.Contains(context.Key)); }
        private void OnBound(LoopCell cell, CellBindContext context) { CellSelectionChanged?.Invoke(cell, selected.Contains(context.Key)); }
        private void OnData()
        {
            removed.Clear();
            foreach (var key in selected) if (!list.TryGetIndex(key, out var index)) removed.Add(key);
            if (removed.Count == 0) return;
            for (var i = 0; i < removed.Count; i++) selected.Remove(removed[i]);
            Notify();
        }
        /// <summary>改变稳定业务 Key 的选择；单选时先清除原选择。</summary>
        /// <param name="key">当前数据中存在的稳定 Key。</param>
        /// <param name="value">是否选中。</param>
        public void SetSelected(string key, bool value)
        {
            if (!list.HasStableKeys) throw new InvalidOperationException("Selection 需要稳定 Key。");
            if (!list.TryGetIndex(key, out var index)) throw new ArgumentException("Key 不存在。", nameof(key));
            var changed = false;
            if (value)
            {
                if (!multiSelect && (selected.Count > 1 || !selected.Contains(key))) { changed = selected.Count > 0; selected.Clear(); }
                changed |= selected.Add(key);
            }
            else changed = selected.Remove(key);
            if (changed) Notify();
        }
        public bool IsSelected(string key) => selected.Contains(key);
        public void ClearSelection() { if (selected.Count == 0) return; selected.Clear(); Notify(); }
        private void Notify()
        {
            // 只更新表现，不重新 Bind，以免取消业务的在途资源加载。
            list.VisitActiveCells(updateCell);
            SelectionChanged?.Invoke();
        }
    }
}
