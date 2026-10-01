using System;
using UnityEngine;

namespace SleepyStudios.LoopScroll
{
    public enum LoopLayout { Vertical, Horizontal, VerticalGrid, HorizontalGrid }
    public enum ScrollAlignment { Start, Center, End }
    public enum ScrollAnchorPolicy { ResetToStart, KeepPosition, KeepFirstVisible, StickToEnd }
    public enum LoopListChangeKind { Insert, Remove, Replace, Move }
    public enum PagingState { Idle, Loading, Completed, Error }
    public enum PagingBoundary { Start, End }

    [Serializable]
    public struct ScrollAnimation
    {
        [Min(0)] public float Duration;
        public static ScrollAnimation Immediate => default;
        /// <summary>使用 unscaled time 和 SmoothStep 平滑定位。</summary>
        /// <param name="duration">动画秒数，非正数表示立即定位。</param>
        public ScrollAnimation(float duration) { Duration = Mathf.Max(0, duration); }
    }

    public struct RefillOptions
    {
        public ScrollAnchorPolicy AnchorPolicy;
        public int? Index;
        public ScrollAlignment Alignment;
        public static RefillOptions Default => default;
        /// <summary>指定重载后的锚点策略，默认回到起点。</summary>
        /// <param name="policy">保持位置、可见项或贴底策略。</param>
        public RefillOptions(ScrollAnchorPolicy policy) { AnchorPolicy = policy; Index = null; Alignment = default; }
        /// <summary>重载完成后立即定位索引；边界处钳制。</summary>
        /// <param name="index">数据索引。</param>
        /// <param name="alignment">相对 Viewport 的对齐。</param>
        public RefillOptions(int index, ScrollAlignment alignment = ScrollAlignment.Start)
        { AnchorPolicy = default; Index = index; Alignment = alignment; }
    }

    public readonly struct VisibleRange : IEquatable<VisibleRange>
    {
        public readonly int First;
        public readonly int Last;
        public bool IsEmpty => First < 0;
        public static VisibleRange Empty => new VisibleRange(-1, -1);
        public VisibleRange(int first, int last) { First = first; Last = last; }
        public bool Equals(VisibleRange other) => First == other.First && Last == other.Last;
        public override bool Equals(object obj) => obj is VisibleRange other && Equals(other);
        public override int GetHashCode() => (First * 397) ^ Last;
        public override string ToString() => IsEmpty ? "Empty" : $"{First}..{Last}";
    }

    public readonly struct LoopListChange
    {
        public readonly LoopListChangeKind Kind;
        public readonly int Index;
        public readonly int Count;
        // Move 的目标索引是先移除块后，再插入的索引。
        public readonly int Destination;
        private LoopListChange(LoopListChangeKind kind, int index, int count, int destination = 0)
        { Kind = kind; Index = index; Count = count; Destination = destination; }
        public static LoopListChange Insert(int index, int count = 1) => new LoopListChange(LoopListChangeKind.Insert, index, count);
        public static LoopListChange Remove(int index, int count = 1) => new LoopListChange(LoopListChangeKind.Remove, index, count);
        public static LoopListChange Replace(int index, int count = 1) => new LoopListChange(LoopListChangeKind.Replace, index, count);
        public static LoopListChange Move(int index, int destination, int count = 1) => new LoopListChange(LoopListChangeKind.Move, index, count, destination);
    }

    [Serializable]
    public sealed class LoopCellPrefab
    {
        public int Type;
        public LoopCell Prefab;
        [Min(0)] public int Prewarm;
    }

    public interface ILoopDataSource
    {
        int Count { get; }
        bool HasStableKeys { get; }
        string GetItemKey(int index);
        int GetCellType(int index);
        float GetEstimatedSize(int index, float crossAxisSize);
        void BindCell(LoopCell cell, int index, CellBindContext context);
        void UnbindCell(LoopCell cell, CellBindContext context);
    }
}
