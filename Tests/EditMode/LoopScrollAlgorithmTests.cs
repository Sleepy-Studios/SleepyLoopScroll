using System;
using NUnit.Framework;
using SleepyStudios.LoopScroll.Internal;

namespace SleepyStudios.LoopScroll.Tests
{
    public sealed class LoopScrollAlgorithmTests
    {
        [Test]
        public void Fenwick_PrefixAndSearchMatchIndependentLinearOracle()
        {
            var random = new Random(734);
            var values = new float[1000];
            for (var i = 0; i < values.Length; i++) values[i] = random.Next(1, 80);
            var tree = new FenwickTree(); tree.Reset(values, values.Length);
            for (var iteration = 0; iteration < 500; iteration++)
            {
                var index = random.Next(values.Length); values[index] = random.Next(1, 80); tree.Set(index, values[index]);
                var total = 0f; for (var i = 0; i < values.Length; i++) total += values[i];
                Assert.That(tree.Total, Is.EqualTo(total));
                var offset = (float)(random.NextDouble() * total);
                var expected = 0; var prefix = values[0];
                while (expected < values.Length - 1 && prefix <= offset) prefix += values[++expected];
                Assert.That(tree.Find(offset), Is.EqualTo(expected));
            }
        }
        [Test]
        public void Fenwick_EmptyAndExactBoundariesAreDefined()
        {
            var tree = new FenwickTree(); tree.Reset(Array.Empty<float>(), 0);
            Assert.That(tree.Find(0), Is.EqualTo(-1));
            tree.Reset(new[] { 10f, 20f, 30f }, 3);
            Assert.That(tree.Find(9), Is.EqualTo(0)); Assert.That(tree.Find(10), Is.EqualTo(1));
            Assert.That(tree.Find(30), Is.EqualTo(2)); Assert.That(tree.Find(100), Is.EqualTo(2));
            tree.Reset(new[] { 3f }, 1); Assert.That(tree.Total, Is.EqualTo(3));
        }
    }
}
