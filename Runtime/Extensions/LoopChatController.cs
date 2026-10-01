using System;
using UnityEngine;

namespace SleepyStudios.LoopScroll
{
    [DisallowMultipleComponent, RequireComponent(typeof(LoopScrollView))]
    public sealed class LoopChatController : MonoBehaviour
    {
        private LoopScrollView list;
        private bool initiallyPositioned;
        public int UnreadCount { get; private set; }
        public event Action<int> UnreadChanged;
        private void Awake() { list = GetComponent<LoopScrollView>(); }
        private void OnEnable() { list.ScrollPositionChanged += OnPosition; list.DataChanged += OnData; OnData(); }
        private void OnDisable() { list.ScrollPositionChanged -= OnPosition; list.DataChanged -= OnData; }
        private void OnData()
        {
            if (!initiallyPositioned && list.Count > 0)
            { initiallyPositioned = true; list.ScrollToCell(list.Count - 1, ScrollAlignment.End); }
        }
        private void OnPosition() { if (list.DistanceToEnd <= 1) SetUnread(0); }
        /// <summary>调用方追加消息后通知列表；浏览历史时保留画面并增加未读。</summary>
        /// <param name="count">新增消息数量，必须为正。</param>
        public void AppendMessages(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            var atBottom = list.DistanceToEnd <= 1;
            list.Append(count, atBottom ? ScrollAnchorPolicy.StickToEnd : ScrollAnchorPolicy.KeepFirstVisible);
            SetUnread(atBottom ? 0 : checked(UnreadCount + count));
        }
        /// <summary>调用方插入历史消息后通知列表；保持当前消息的可见位置。</summary>
        /// <param name="count">前插历史消息数量。</param>
        public void PrependHistory(int count) { list.Prepend(count, ScrollAnchorPolicy.KeepFirstVisible); }
        /// 回到底部并清除未读；空列表不定位。
        public void JumpToLatest() { if (list.Count > 0) list.ScrollToCell(list.Count - 1, ScrollAlignment.End); SetUnread(0); }
        private void SetUnread(int value) { if (UnreadCount == value) return; UnreadCount = value; UnreadChanged?.Invoke(value); }
    }
}
