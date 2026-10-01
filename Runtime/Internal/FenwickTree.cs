using System;

namespace SleepyStudios.LoopScroll.Internal
{
    internal sealed class FenwickTree
    {
        private float[] tree = Array.Empty<float>();
        private float[] values = Array.Empty<float>();
        public int Count { get; private set; }
        public float Total => Prefix(Count);
        public float this[int index] => values[index];
        public void Reset(float[] sizes, int count)
        {
            Count = count;
            if (tree.Length < count + 1) tree = new float[count + 1];
            if (values.Length < count) values = new float[count];
            Array.Clear(tree, 0, count + 1);
            for (var i = 0; i < count; i++)
            {
                values[i] = sizes[i];
                tree[i + 1] += sizes[i];
                var parent = (i + 1) + ((i + 1) & -(i + 1));
                if (parent <= count) tree[parent] += tree[i + 1];
            }
        }
        public void Set(int index, float value)
        {
            var delta = value - values[index];
            values[index] = value;
            for (var i = index + 1; i <= Count; i += i & -i) tree[i] += delta;
        }
        public float Prefix(int exclusiveEnd)
        {
            var sum = 0f;
            for (var i = exclusiveEnd; i > 0; i -= i & -i) sum += tree[i];
            return sum;
        }
        // 第一个累计结束位置大于 offset 的项；边界返回最后一项。
        public int Find(float offset)
        {
            if (Count == 0) return -1;
            var index = 0;
            var sum = 0f;
            var bit = 1;
            while (bit <= Count / 2) bit <<= 1;
            for (; bit != 0; bit >>= 1)
            {
                var next = index + bit;
                if (next <= Count && sum + tree[next] <= offset)
                { sum += tree[next]; index = next; }
            }
            return Math.Min(index, Count - 1);
        }
    }
}
