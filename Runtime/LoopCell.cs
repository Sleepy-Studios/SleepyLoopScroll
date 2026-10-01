using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SleepyStudios.LoopScroll
{
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class LoopCell : MonoBehaviour, IPointerClickHandler
    {
        private long version;
        private bool bound;
        private CancellationTokenSource cancellation;
        private RectTransform rect;
        private Action<LoopCell, CellBindContext> click;
        public RectTransform RectTransform => rect != null ? rect : (rect = (RectTransform)transform);
        public CellBindContext Context { get; private set; }
        internal ILoopDataSource BoundSource;
        internal int PoolType;
        internal int Slot;
        internal bool Measured;

        internal CellBindContext BeginBind(string key, int index, ILoopDataSource source, Action<LoopCell, CellBindContext> onClick)
        {
            version++;
            bound = true;
            BoundSource = source;
            click = onClick;
            Measured = false;
            Context = new CellBindContext(this, key, version, index);
            return Context;
        }

        internal void EndBind()
        {
            var previous = Context;
            var source = BoundSource;
            bound = false;
            version++;
            click = null;
            BoundSource = null;
            if (cancellation != null)
            {
                try { cancellation.Cancel(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
                finally { cancellation.Dispose(); cancellation = null; }
            }
            if (source != null)
            {
                try { source.UnbindCell(this, previous); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        internal bool IsCurrent(long candidate) => bound && version == candidate;
        internal CancellationToken GetToken(long candidate)
        {
            if (!IsCurrent(candidate)) return new CancellationToken(true);
            if (cancellation == null) cancellation = new CancellationTokenSource();
            return cancellation.Token;
        }
        /// <summary>仅在有效绑定且未形成拖动时抛出点击。</summary>
        /// <param name="eventData">Unity 指针事件。</param>
        public void OnPointerClick(PointerEventData eventData)
        { if (bound && !eventData.dragging) click?.Invoke(this, Context); }
        private void OnDestroy() { EndBind(); }
    }
}
