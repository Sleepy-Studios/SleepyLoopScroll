using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SleepyStudios.LoopScroll.Tests
{
    public sealed class LoopScrollPlayModeTests
    {
        private GameObject canvas;
        private LoopScrollView list;
        private ScrollRect scroll;
        private LoopCell template;
        private readonly List<string> items = new List<string>();
        private static readonly Func<object, string> KeySelector = value => (string)value;
        private static readonly Action<Image, int, CellBindContext> Bind = (cell, value, context) => cell.color = Color.white;

        private void Create(LoopLayout mode = LoopLayout.Vertical, bool dynamic = false, int prewarm = 40, bool registerBinding = true)
        {
            canvas = new GameObject("LoopTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var root = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(canvas.transform, false); root.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 200);
            scroll = root.GetComponent<ScrollRect>(); scroll.inertia = false;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(root.transform, false); viewport.sizeDelta = new Vector2(300, 200);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport, false);
            var cell = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(LoopCell));
            cell.transform.SetParent(root.transform, false); cell.SetActive(false); template = cell.GetComponent<LoopCell>();
            scroll.viewport = viewport; scroll.content = content;
            list = root.AddComponent<LoopScrollView>();
            list.Configure(scroll, new[] { new LoopCellPrefab { Type = 0, Prefab = template, Prewarm = prewarm } }, mode, new Vector2(100, 40), dynamic);
            if (registerBinding) list.RegisterCellBinding(Bind);
        }
        private void Populate(int count)
        { items.Clear(); for (var i = 0; i < count; i++) items.Add("item:" + i); }
        private void Submit(RefillOptions options = default) { list.SetTotalCount(items, options, KeySelector); }
        [TearDown]
        public void Cleanup() { if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas); items.Clear(); }

        [UnityTest]
        public IEnumerator HundredThousandItemsRemainBoundedAndReuseWarmedPool()
        {
            Create(); Populate(100000); Submit(); yield return null;
            Assert.That(list.ActiveCellCount, Is.LessThan(20)); var created = list.CreatedCellCount;
            for (var i = 0; i < 100; i++) { list.ScrollToCell(i * 987); yield return null; }
            Assert.That(list.CreatedCellCount, Is.EqualTo(created));
            Assert.That(list.VisibleRange.First, Is.GreaterThan(90000));
        }
        [UnityTest]
        public IEnumerator RegisteredBindingSupportsRepeatedSubmissionNullAndInactiveRecovery()
        {
            Create(registerBinding: false); Populate(100);
            Assert.Throws<InvalidOperationException>(() => list.SetTotalCount(items));
            var unbound = 0;
            list.RegisterCellBinding<Image>((cell, index, context) => Assert.That(context.Key, Is.EqualTo(items[index])),
                (cell, context) => { Assert.That(context.IsCurrent, Is.False); unbound++; });
            list.SetTotalCount(items, getItemKey: item => (string)item); yield return null;
            list.ScrollToCell(40, ScrollAlignment.Center);
            var offset = list.Offset; var first = list.GetItemKey(list.VisibleRange.First);
            items.Insert(0, "history");
            list.SetTotalCount(items, new RefillOptions(ScrollAnchorPolicy.KeepFirstVisible), item => (string)item);
            Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo(first));
            Assert.That(list.Offset, Is.EqualTo(offset + 40).Within(1));
            var previous = list.GetVisibleCell(list.VisibleRange.First).Context;
            list.RefreshCells(); Assert.That(previous.IsCurrent, Is.False);
            list.SetTotalCount(items, getItemKey: item => (string)item); Assert.That(list.Offset, Is.Zero.Within(1));
            list.RefillCells(new RefillOptions(20)); Assert.That(list.Offset, Is.EqualTo(800).Within(1));
            list.RefillCells(new RefillOptions(ScrollAnchorPolicy.StickToEnd)); Assert.That(list.DistanceToEnd, Is.Zero.Within(1));
            list.SetTotalCount(null); Assert.That(list.Count, Is.Zero); list.RefillCells();
            list.gameObject.SetActive(false);
            list.SetTotalCount(items, new RefillOptions(10), item => (string)item);
            Assert.That(list.ActiveCellCount, Is.Zero);
            list.gameObject.SetActive(true); yield return null; yield return null;
            Assert.That(list.Count, Is.EqualTo(101)); Assert.That(list.Offset, Is.EqualTo(400).Within(1));
            Assert.That(unbound, Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator ReregisterDoesNotReplaceUnbindOfExistingCells()
        {
            Create(); Populate(20); var oldUnbind = 0; var nextUnbind = 0;
            list.RegisterCellBinding<Image>((cell, index, context) => { }, (cell, context) => oldUnbind++);
            list.SetTotalCount(items); yield return null; var activeCount = list.ActiveCellCount;
            list.RegisterCellBinding<Image>((cell, index, context) => { }, (cell, context) => nextUnbind++);
            list.SetTotalCount(items);
            Assert.That(oldUnbind, Is.EqualTo(activeCount)); Assert.That(nextUnbind, Is.Zero);
            list.SetTotalCount(null); Assert.That(nextUnbind, Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator HorizontalScrollAndAlignmentsUseActualViewport()
        {
            Create(LoopLayout.Horizontal); Populate(100); Submit(); yield return null;
            list.ScrollToCell(50, ScrollAlignment.Start); Assert.That(list.Offset, Is.EqualTo(5000).Within(1));
            list.ScrollToCell(50, ScrollAlignment.Center); Assert.That(list.Offset, Is.EqualTo(4900).Within(1));
            list.ScrollToCell(50, ScrollAlignment.End, new ScrollAnimation(.05f)); yield return new WaitForSecondsRealtime(.1f);
            Assert.That(list.Offset, Is.EqualTo(4800).Within(1));
            list.ScrollToCell(99, ScrollAlignment.Start); Assert.That(list.Offset, Is.EqualTo(list.MaxOffset).Within(1));
        }
        [UnityTest]
        public IEnumerator GridResizeReflowsColumnsAndRetainsAnchor()
        {
            Create(LoopLayout.VerticalGrid); Populate(100); Submit(); yield return null;
            list.ScrollToCell(30); var key = list.GetItemKey(list.VisibleRange.First);
            scroll.viewport.sizeDelta = new Vector2(200, 200); yield return null; yield return null;
            Assert.That(list.VisibleRange.First, Is.EqualTo(30));
            Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo(key));
            Assert.That(list.GetVisibleCell(31).RectTransform.anchoredPosition.x, Is.EqualTo(100).Within(1));
        }
        [UnityTest]
        public IEnumerator ZeroInactiveSubmissionAndReenableBuildCells()
        {
            Create(); Populate(0); Submit(); yield return null; Assert.That(list.ActiveCellCount, Is.Zero);
            list.gameObject.SetActive(false); Populate(20); list.RefillCells();
            Assert.That(list.ActiveCellCount, Is.Zero); list.gameObject.SetActive(true); yield return null; yield return null;
            Assert.That(list.ActiveCellCount, Is.GreaterThan(0));
            items.Clear(); list.RefillCells(); Assert.That(list.ActiveCellCount, Is.Zero);
            Populate(30); list.RefillCells(); yield return null; Assert.That(list.ActiveCellCount, Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator ComponentAwakeCompletesBeforeBindingAnInactivePrefab()
        {
            Create(); template.gameObject.AddComponent<AwakeInitializedCell>(); Populate(30);
            list.RegisterCellBinding<AwakeInitializedCell>((cell, index, context) => Assert.That(cell.Initialized, Is.True)); Submit();
            yield return null; Assert.That(list.ActiveCellCount, Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator PrependRemoveReplaceMoveAndRefillPreserveStableIdentity()
        {
            Create(); Populate(100); Submit(); yield return null;
            list.ScrollToOffset(807); var key = list.GetItemKey(list.VisibleRange.First);
            items.Insert(0, "history"); list.Prepend(1);
            Assert.That(list.Offset, Is.EqualTo(847).Within(1)); Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo(key));
            items.RemoveAt(0); list.ApplyChanges(new[] { LoopListChange.Remove(0) }); Assert.That(list.Offset, Is.EqualTo(807).Within(1));
            var moved = items[0]; items.RemoveAt(0); items.Insert(50, moved); list.ApplyChanges(new[] { LoopListChange.Move(0, 50) });
            Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo(key));
            var anchor = list.VisibleRange.First; items.RemoveAt(anchor); list.ApplyChanges(new[] { LoopListChange.Remove(anchor) });
            Assert.That(list.Offset % 40, Is.EqualTo(7).Within(1));
            items[0] = "replacement"; list.ApplyChanges(new[] { LoopListChange.Replace(0) });
            list.RefillCells(new RefillOptions(ScrollAnchorPolicy.KeepPosition)); yield return null;
            Assert.That(list.Offset % 40, Is.EqualTo(7).Within(1));
        }
        [UnityTest]
        public IEnumerator PrependDuringDragRebasesNativePointerWithoutJump()
        {
            Create(); Populate(100); Submit(); yield return null; list.ScrollToCell(20);
            var events = new GameObject("PointerEvents", typeof(EventSystem));
            try
            {
                var data = new PointerEventData(events.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left, position = new Vector2(200, 200) };
                scroll.OnBeginDrag(data); list.OnBeginDrag(data);
                items.Insert(0, "history"); list.Prepend(1); Assert.That(list.Offset, Is.EqualTo(840).Within(1));
                data.position += new Vector2(0, 40); data.delta = new Vector2(0, 40); scroll.OnDrag(data);
                Assert.That(list.Offset, Is.EqualTo(880).Within(1)); scroll.OnEndDrag(data); list.OnEndDrag(data);
            }
            finally { UnityEngine.Object.DestroyImmediate(events); }
        }
        [UnityTest]
        public IEnumerator UnbindCannotReenterSnapshotUpdate()
        {
            Create(); Populate(30); var rejected = 0;
            list.RegisterCellBinding(Bind, (cell, context) => { Assert.Throws<InvalidOperationException>(() => list.RefillCells()); rejected++; }); Submit();
            yield return null; items.Insert(0, "new"); list.Prepend(1);
            Assert.That(rejected, Is.GreaterThan(0)); Assert.That(list.Count, Is.EqualTo(31));
        }
        [UnityTest]
        public IEnumerator InvalidBatchAndDuplicateKeyDoNotDestroyExistingDisplay()
        {
            Create(); Populate(50); Submit(); yield return null;
            var context = list.GetVisibleCell(0).Context;
            Assert.Throws<InvalidOperationException>(() => list.ApplyChanges(new[] { LoopListChange.Insert(0) }));
            Assert.That(context.IsCurrent, Is.True); var count = list.Count;
            items[1] = items[0]; Assert.Throws<InvalidOperationException>(() => list.RefillCells());
            Assert.That(list.Count, Is.EqualTo(count)); Assert.That(context.IsCurrent, Is.True);
        }
        [UnityTest]
        public IEnumerator AsyncRecycleCancelsTokenAndInvalidatesUncooperativeContinuation()
        {
            Create(); Populate(100); Submit(); yield return null;
            var cell = list.GetVisibleCell(0); var old = cell.Context;
            long firstTokenBytes, secondTokenBytes; CancellationToken token, sameToken;
            using (var probe = new GcAllocationProbe())
            {
                probe.Begin(); token = old.CancellationToken; firstTokenBytes = probe.End();
                probe.Begin(); sameToken = old.CancellationToken; secondTokenBytes = probe.End();
            }
            Assert.That(firstTokenBytes, Is.GreaterThan(0)); Assert.That(secondTokenBytes, Is.Zero); Assert.That(sameToken, Is.EqualTo(token));
            Debug.Log($"SleepyLoopScroll token allocation: firstBytes={firstTokenBytes}, repeatedBytes={secondTokenBytes}");
            list.ScrollToCell(80); yield return null;
            Assert.That(token.IsCancellationRequested, Is.True); Assert.That(old.IsCurrent, Is.False);
            Assert.That(old.CancellationToken.IsCancellationRequested, Is.True);
            var wrote = false; Action lateResult = () => { if (old.IsCurrent) { cell.GetComponent<Image>().color = Color.red; wrote = true; } };
            lateResult(); Assert.That(wrote, Is.False);
            var current = list.GetVisibleCell(80).Context;
            list.RefreshCells(); Assert.That(current.IsCurrent, Is.False);
            current = list.GetVisibleCell(80).Context; list.gameObject.SetActive(false); Assert.That(current.IsCurrent, Is.False);
        }
        [UnityTest]
        public IEnumerator DynamicHeightChangesAndCrossAxisResizeKeepAnchor()
        {
            Create(dynamic: true); Populate(100);
            list.RegisterCellBinding<LayoutElement>((cell, index, context) => cell.preferredHeight = 60); Submit();
            yield return null; yield return null; list.ScrollToCell(50); yield return null; yield return null;
            var key = list.GetItemKey(list.VisibleRange.First); var cell = list.GetVisibleCell(list.VisibleRange.First);
            cell.GetComponent<LayoutElement>().preferredHeight = 120; list.InvalidateCellSize(cell.Context.Index);
            yield return null; yield return null;
            Assert.That(cell.RectTransform.rect.height, Is.EqualTo(120).Within(1));
            Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo(key));
            scroll.viewport.sizeDelta = new Vector2(200, 200); yield return null; yield return null;
            Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo(key));
        }
        [UnityTest]
        public IEnumerator DynamicCenterAlignmentIsCorrectAfterFirstMeasurement()
        {
            Create(dynamic: true); Populate(100);
            list.RegisterCellBinding<LayoutElement>((cell, index, context) => cell.preferredHeight = 80); Submit();
            yield return null; yield return null; list.ScrollToCell(50, ScrollAlignment.Center); yield return null; yield return null; yield return null;
            var cell = list.GetVisibleCell(50);
            Assert.That(cell.RectTransform.anchoredPosition.y + list.Offset, Is.EqualTo(-60).Within(1));
        }
        [UnityTest]
        public IEnumerator RefillTargetUsesMeasuredDynamicAlignment()
        {
            Create(dynamic: true); Populate(100);
            list.RegisterCellBinding<LayoutElement>((cell, index, context) => cell.preferredHeight = 80); Submit(new RefillOptions(50, ScrollAlignment.Center));
            yield return null; yield return null; yield return null;
            var cell = list.GetVisibleCell(50);
            Assert.That(cell.RectTransform.anchoredPosition.y + list.Offset, Is.EqualTo(-60).Within(1));
        }
        [UnityTest]
        public IEnumerator ChatInitialBottomHistoryPrependUnreadAndFollow()
        {
            Create(); var chat = list.gameObject.AddComponent<LoopChatController>(); Populate(100); Submit(); yield return null; yield return null;
            Assert.That(list.DistanceToEnd, Is.LessThanOrEqualTo(1));
            list.ScrollToCell(20); items.Insert(0, "history"); chat.PrependHistory(1);
            Assert.That(list.GetItemKey(list.VisibleRange.First), Is.EqualTo("item:20"));
            items.Add("new"); chat.AppendMessages(1); Assert.That(chat.UnreadCount, Is.EqualTo(1));
            chat.JumpToLatest(); Assert.That(chat.UnreadCount, Is.Zero);
            items.Add("new2"); chat.AppendMessages(1); yield return null;
            Assert.That(list.DistanceToEnd, Is.LessThanOrEqualTo(1));
        }
        [UnityTest]
        public IEnumerator PagingDeduplicatesAndRequiresExplicitRetry()
        {
            Create(); Populate(20); Submit(); var paging = list.gameObject.AddComponent<LoopPagingTrigger>();
            paging.Configure(true, false); var requests = 0; paging.LoadRequested += boundary => requests++;
            paging.Evaluate(); paging.Evaluate(); Assert.That(requests, Is.EqualTo(1));
            paging.Fail(PagingBoundary.Start); paging.Evaluate(); Assert.That(requests, Is.EqualTo(1));
            paging.Retry(PagingBoundary.Start); Assert.That(requests, Is.EqualTo(2));
            paging.Complete(PagingBoundary.Start, false); paging.Evaluate(); Assert.That(requests, Is.EqualTo(2));
            Assert.That(paging.StartState, Is.EqualTo(PagingState.Completed)); yield return null;
        }
        [UnityTest]
        public IEnumerator SelectionSurvivesRefreshAndClearsDeletedKeys()
        {
            Create(); Populate(40); Submit(); var selection = list.gameObject.AddComponent<LoopSelectionController>();
            selection.SetSelected("item:2", true); list.RefreshCells(); Assert.That(selection.IsSelected("item:2"), Is.True);
            selection.SetSelected("item:3", true); Assert.That(selection.IsSelected("item:2"), Is.False);
            selection.MultiSelect = true; selection.SetSelected("item:4", true); Assert.That(selection.SelectedKeys.Count, Is.EqualTo(2));
            items.RemoveAt(3); list.ApplyChanges(new[] { LoopListChange.Remove(3) }); Assert.That(selection.IsSelected("item:3"), Is.False); yield return null;
        }
        [UnityTest]
        public IEnumerator CarouselZeroOneTwoManyAndRepeatedRecenter()
        {
            Create(LoopLayout.Horizontal); var carousel = list.gameObject.AddComponent<LoopCarouselController>(); carousel.Configure(300, 0, 0);
            Populate(0); Submit(); Assert.That(carousel.CurrentPage, Is.EqualTo(-1));
            Populate(1); list.RefillCells(); yield return null; Assert.That(list.IsLooping, Is.False);
            Populate(2); list.RefillCells(); yield return null; yield return null; Assert.That(list.IsLooping, Is.True);
            for (var i = 0; i < 6; i++) { carousel.Next(); yield return null; yield return null; }
            Assert.That(carousel.CurrentPage, Is.EqualTo(0)); Assert.That(Mathf.Abs(list.Offset), Is.LessThan(1000));
            Populate(5); list.RefillCells(); yield return null; carousel.SetPage(4); yield return null; yield return null;
            Assert.That(carousel.CurrentPage, Is.EqualTo(4)); carousel.Previous(); yield return null; yield return null;
            Assert.That(carousel.CurrentPage, Is.EqualTo(3));
        }
        [UnityTest]
        public IEnumerator CarouselHighSpeedReverseDragSnapsAndAutoPlayResumes()
        {
            Create(LoopLayout.Horizontal); var carousel = list.gameObject.AddComponent<LoopCarouselController>();
            carousel.Configure(300, 0, 0); Populate(5); Submit(); yield return null; yield return null;
            var eventObject = new GameObject("DragEvents", typeof(EventSystem));
            try
            {
                var data = new PointerEventData(eventObject.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left, position = new Vector2(100, 100) };
                list.OnBeginDrag(data); scroll.OnBeginDrag(data);
                data.position = new Vector2(-10400, 100); data.delta = new Vector2(-10500, 0); scroll.OnDrag(data);
                Assert.That(list.Offset, Is.GreaterThan(10000));
                scroll.OnEndDrag(data); list.OnEndDrag(data); scroll.StopMovement(); yield return null; yield return null;
                Assert.That(carousel.CurrentPage, Is.EqualTo(0)); Assert.That(Mathf.Abs(list.Offset), Is.LessThan(1500));
                list.OnBeginDrag(data); scroll.OnBeginDrag(data); data.position = new Vector2(-7400, 100); data.delta = new Vector2(3000, 0); scroll.OnDrag(data);
                scroll.OnEndDrag(data); list.OnEndDrag(data); scroll.StopMovement(); yield return null; yield return null;
                Assert.That(carousel.CurrentPage, Is.EqualTo(0));
                carousel.Configure(300, .05f, 0); yield return new WaitForSecondsRealtime(.08f); yield return null;
                Assert.That(carousel.CurrentPage, Is.Not.EqualTo(0));
            }
            finally { UnityEngine.Object.DestroyImmediate(eventObject); }
        }
        [UnityTest]
        public IEnumerator PagingBoundariesHaveIndependentLoadingAndCompletion()
        {
            Create(); Populate(1); Submit(); var paging = list.gameObject.AddComponent<LoopPagingTrigger>(); paging.Configure(true, true);
            var starts = 0; var ends = 0; paging.LoadRequested += boundary => { if (boundary == PagingBoundary.Start) starts++; else ends++; };
            paging.Evaluate(); paging.Evaluate(); Assert.That(starts, Is.EqualTo(1)); Assert.That(ends, Is.EqualTo(1));
            paging.Fail(PagingBoundary.Start); paging.Complete(PagingBoundary.End, false);
            paging.Retry(PagingBoundary.Start); Assert.That(starts, Is.EqualTo(2)); Assert.That(ends, Is.EqualTo(1));
            Assert.That(paging.EndState, Is.EqualTo(PagingState.Completed)); yield return null;
        }
        [UnityTest]
        public IEnumerator ScrollbarControlsAndReflectsHorizontalPosition()
        {
            Create(LoopLayout.Horizontal); var bar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar)).GetComponent<Scrollbar>();
            bar.transform.SetParent(canvas.transform); scroll.horizontalScrollbar = bar;
            Populate(100); Submit(); yield return null; list.ScrollToCell(50); yield return null; yield return null;
            Assert.That(bar.value, Is.EqualTo(5000f / 9700).Within(.001f));
            bar.value = .5f; yield return null; yield return null; Assert.That(list.Offset, Is.EqualTo(4850).Within(1));
        }
        [UnityTest]
        public IEnumerator ZeroViewportRecoversAndMissingComponentIsSafelyRejected()
        {
            Create(); scroll.viewport.sizeDelta = Vector2.zero; Populate(20); Submit(); yield return null;
            Assert.That(list.ActiveCellCount, Is.Zero); scroll.viewport.sizeDelta = new Vector2(300, 200); yield return null; yield return null;
            Assert.That(list.ActiveCellCount, Is.GreaterThan(0));
            items.Clear(); list.RefillCells(); Populate(1);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Cell 绑定失败.*"));
            list.RegisterCellBinding<Text>((cell, index, context) => { }); Submit();
            Assert.That(list.ActiveCellCount, Is.Zero);
        }
        [UnityTest]
        public IEnumerator NestedRouterTransfersOneGestureToParentAtBoundary()
        {
            Create(); Populate(50); Submit(); yield return null;
            var parentObject = new GameObject("ParentScroll", typeof(RectTransform), typeof(ScrollRect)); parentObject.transform.SetParent(canvas.transform);
            var parent = parentObject.GetComponent<ScrollRect>(); parent.viewport = scroll.viewport; parent.content = scroll.content;
            var router = scroll.viewport.gameObject.AddComponent<NestedScrollRouter>(); router.Configure(scroll, parent);
            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            try
            {
                var data = new PointerEventData(eventObject.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left, delta = new Vector2(0, -20) };
                router.OnInitializePotentialDrag(data); router.OnBeginDrag(data); Assert.That(list.IsDragging, Is.True);
                router.OnDrag(data); Assert.That(list.IsDragging, Is.False); router.OnEndDrag(data); Assert.That(list.IsDragging, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(eventObject); }
        }
        [UnityTest]
        public IEnumerator MultipleTypesUseSeparatePoolsAndChangeTypesOnReplace()
        {
            Create();
            var root = list.gameObject; UnityEngine.Object.DestroyImmediate(list);
            list = root.AddComponent<LoopScrollView>();
            list.Configure(scroll, new[] { new LoopCellPrefab { Type = 0, Prefab = template, Prewarm = 20 }, new LoopCellPrefab { Type = 1, Prefab = template, Prewarm = 20 } }, LoopLayout.Vertical, new Vector2(100, 40));
            var source = new AlternatingSource(); list.SetDataSource(source); yield return null;
            Assert.That(list.GetVisibleCell(0).name, Is.Not.Null); var old = list.GetVisibleCell(0).Context;
            source.Flip = true; list.ApplyChanges(new[] { LoopListChange.Replace(0, source.Count) });
            Assert.That(old.IsCurrent, Is.False); var created = list.CreatedCellCount;
            list.ScrollToCell(40); yield return null; Assert.That(list.CreatedCellCount, Is.EqualTo(created));
        }
        private sealed class AlternatingSource : ILoopDataSource
        {
            public bool Flip; public int Count => 100; public bool HasStableKeys => true;
            public string GetItemKey(int index) => index.ToString(); public int GetCellType(int index) => (index + (Flip ? 1 : 0)) % 2;
            public float GetEstimatedSize(int index, float crossAxisSize) => 40;
            public void BindCell(LoopCell cell, int index, CellBindContext context) { }
            public void UnbindCell(LoopCell cell, CellBindContext context) { }
        }
        private static void AssertCompleted(ScrollResult result)
        { Assert.That(result.Status, Is.EqualTo(ScrollStatus.Completed)); Assert.That(result.CancelReason, Is.EqualTo(ScrollCancelReason.None)); }
        private static void AssertCanceled(ScrollResult result, ScrollCancelReason reason)
        { Assert.That(result.Status, Is.EqualTo(ScrollStatus.Canceled)); Assert.That(result.CancelReason, Is.EqualTo(reason)); }
        private static IEnumerator UntilFinished(Func<bool> finished)
        {
            var until = Time.realtimeSinceStartup + 3;
            while (!finished() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(finished(), Is.True, "定位未在布局稳定后终止");
        }
        [UnityTest]
        public IEnumerator OffsetMatrixCoversListsGridsAlignmentsAnimationAndClamping()
        {
            foreach (var mode in new[] { LoopLayout.Vertical, LoopLayout.Horizontal, LoopLayout.VerticalGrid, LoopLayout.HorizontalGrid })
            {
                Create(mode); Populate(200); Submit(); yield return null;
                var axis = list.IsVertical ? 40f : 100f;
                var viewport = list.IsVertical ? 200f : 300f;
                var lanes = mode == LoopLayout.VerticalGrid ? 3 : mode == LoopLayout.HorizontalGrid ? 5 : 1;
                foreach (ScrollAlignment alignment in Enum.GetValues(typeof(ScrollAlignment)))
                foreach (var pixels in new[] { -25f, 25f })
                foreach (var duration in new[] { 0f, .03f })
                {
                    list.ScrollToCell(0);
                    var target = 60 / lanes * axis - (alignment == ScrollAlignment.Start ? 0 :
                        (viewport - axis) * (alignment == ScrollAlignment.Center ? .5f : 1)) - pixels;
                    var calls = 0;
                    list.ScrollToCell(60, alignment, new ScrollAnimation(duration), pixels, result =>
                    { AssertCompleted(result); Assert.That(list.Offset, Is.EqualTo(target).Within(1)); Assert.That(list.GetVisibleCell(60), Is.Not.Null); calls++; });
                    if (duration == 0) Assert.That(calls, Is.EqualTo(1));
                    yield return UntilFinished(() => calls > 0);
                    Assert.That(calls, Is.EqualTo(1)); Assert.That(list.IsAnimating, Is.False);
                }
                var boundaries = 0;
                list.ScrollToCell(0, offsetPixels: 1000, onFinished: result => { AssertCompleted(result); boundaries++; });
                Assert.That(list.Offset, Is.Zero.Within(1));
                list.ScrollToCell(199, ScrollAlignment.End, offsetPixels: -1000, onFinished: result => { AssertCompleted(result); boundaries++; });
                Assert.That(list.Offset, Is.EqualTo(list.MaxOffset).Within(1)); Assert.That(boundaries, Is.EqualTo(2));
                Cleanup();
            }
        }
        [UnityTest]
        public IEnumerator InvalidRequestsPreserveAnimationAndSuccessfulReplacementNotifiesOnce()
        {
            Create(); Populate(100); Submit(); yield return null;
            var oldCalls = 0; var newCalls = 0;
            list.ScrollToCell(60, animation: new ScrollAnimation(10), onFinished: result => { AssertCanceled(result, ScrollCancelReason.Replaced); oldCalls++; });
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ScrollToCell(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ScrollToCell(100));
            foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.ScrollToCell(20, offsetPixels: invalid));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.ScrollToOffset(invalid));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.ScrollToCell(20, animation: new ScrollAnimation(invalid)));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.ScrollToOffset(100, new ScrollAnimation { Duration = invalid }));
            }
            Assert.That(oldCalls, Is.Zero); Assert.That(list.IsAnimating, Is.True);
            list.ScrollToOffset(640, new ScrollAnimation(-1), result => { AssertCompleted(result); newCalls++; });
            Assert.That(oldCalls, Is.EqualTo(1)); Assert.That(newCalls, Is.EqualTo(1));
            list.CancelAnimation(); yield return null; Assert.That(newCalls, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator ExplicitCancelStopsPositionAndCancelsWaitingRequestOnce()
        {
            Create(); Populate(100); Submit(); yield return null;
            var calls = 0;
            Action<ScrollResult> canceled = result => { AssertCanceled(result, ScrollCancelReason.ExplicitCancel); calls++; };
            list.ScrollToCell(80, animation: new ScrollAnimation(1), onFinished: canceled); yield return null;
            list.CancelAnimation(); var stopped = list.Offset; list.CancelAnimation();
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(list.Offset, Is.EqualTo(stopped).Within(1)); Assert.That(calls, Is.EqualTo(1));
            list.gameObject.SetActive(false); list.ScrollToOffset(1000, new ScrollAnimation(1), canceled);
            Assert.That(list.IsAnimating, Is.False); list.CancelAnimation(); list.gameObject.SetActive(true);
            yield return null; yield return null; Assert.That(calls, Is.EqualTo(2)); Assert.That(list.Offset, Is.EqualTo(stopped).Within(1));
        }
        [UnityTest]
        public IEnumerator DragCancellationHandsPositionToNativeScrollRect()
        {
            Create(); Populate(100); Submit(); yield return null; list.ScrollToCell(20);
            var events = new GameObject("ScrollRequestEvents", typeof(EventSystem));
            try
            {
                var calls = 0;
                list.ScrollToCell(80, animation: new ScrollAnimation(1), onFinished: result => { AssertCanceled(result, ScrollCancelReason.DragStarted); calls++; });
                var pointer = new PointerEventData(events.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left, position = new Vector2(200, 200) };
                scroll.OnBeginDrag(pointer); list.OnBeginDrag(pointer);
                var before = list.Offset; pointer.position += Vector2.up * 40; scroll.OnDrag(pointer);
                Assert.That(list.Offset, Is.EqualTo(before + 40).Within(1));
                scroll.OnEndDrag(pointer); list.OnEndDrag(pointer); yield return null;
                Assert.That(calls, Is.EqualTo(1)); Assert.That(list.IsAnimating, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(events); }
        }
        [UnityTest]
        public IEnumerator DataChangesCancelOnlyAfterSuccessfulValidationAndPermitCallbackRefill()
        {
            Create(); Populate(100); Submit(); yield return null;
            var calls = 0;
            list.ScrollToCell(80, animation: new ScrollAnimation(10), onFinished: result =>
            { AssertCanceled(result, ScrollCancelReason.DataChanged); Assert.That(list.Count, Is.EqualTo(101)); calls++; list.ScrollToCell(30); });
            items.Add(items[0]); Assert.Throws<InvalidOperationException>(() => list.RefillCells()); items.RemoveAt(items.Count - 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RefillCells(new RefillOptions(999)));
            Assert.That(calls, Is.Zero); list.RefreshCells(); Assert.That(list.IsAnimating, Is.True);
            items.Add("added"); list.Append(1); Assert.That(calls, Is.EqualTo(1)); Assert.That(list.Offset, Is.EqualTo(1200).Within(1));
            foreach (var operation in new Action[] { () => Submit(), () => list.RefillCells(), () => list.ApplyChanges(new[] { LoopListChange.Replace(0) }) })
            {
                list.ScrollToCell(80, animation: new ScrollAnimation(10), onFinished: result =>
                { AssertCanceled(result, ScrollCancelReason.DataChanged); calls++; list.RefillCells(new RefillOptions(20)); });
                operation(); Assert.That(list.Offset, Is.EqualTo(800).Within(1));
            }
            Assert.That(calls, Is.EqualTo(4)); yield return null;
        }
        [UnityTest]
        public IEnumerator InactiveAndZeroAxisRequestsRetainAlignmentOffsetAndFullAnimationDuration()
        {
            foreach (var state in new[] { 0, 1, 2 })
            {
                Create(); Populate(100); Submit(); yield return null;
                if (state == 0) list.gameObject.SetActive(false);
                else scroll.viewport.sizeDelta = state == 1 ? new Vector2(0, 200) : new Vector2(300, 0);
                var replaced = 0; var completed = 0;
                list.ScrollToCell(30, onFinished: result => { AssertCanceled(result, ScrollCancelReason.Replaced); replaced++; });
                list.ScrollToCell(50, ScrollAlignment.Center, new ScrollAnimation(.3f), 25, result => { AssertCompleted(result); completed++; });
                yield return null; Assert.That(replaced, Is.EqualTo(1)); Assert.That(completed, Is.Zero); Assert.That(list.IsAnimating, Is.False);
                scroll.viewport.sizeDelta = new Vector2(300, 200); list.gameObject.SetActive(true);
                yield return null; yield return null;
                Assert.That(list.IsAnimating, Is.True); Assert.That(completed, Is.Zero);
                yield return UntilFinished(() => completed > 0);
                Assert.That(list.Offset, Is.EqualTo(1895).Within(1)); Assert.That(completed, Is.EqualTo(1)); Cleanup();
            }
            Create(); Populate(100); Submit(); list.gameObject.SetActive(false);
            var offsetCalls = 0; list.ScrollToOffset(777, new ScrollAnimation(.15f), result => { AssertCompleted(result); offsetCalls++; });
            list.gameObject.SetActive(true); yield return UntilFinished(() => offsetCalls > 0);
            Assert.That(list.Offset, Is.EqualTo(777).Within(1)); Assert.That(offsetCalls, Is.EqualTo(1));
            list.gameObject.SetActive(false); Submit(new RefillOptions(10)); list.gameObject.SetActive(true);
            var immediate = 0;
            list.ScrollToCell(20, onFinished: result => { AssertCompleted(result); immediate++; });
            yield return null; yield return null;
            Assert.That(immediate, Is.EqualTo(1)); Assert.That(list.Offset, Is.EqualTo(800).Within(1));
        }
        [UnityTest]
        public IEnumerator DisabledDestroyedAndUnavailableRequestsTerminateExactlyOnce()
        {
            foreach (var kind in new[] { 0, 1, 2, 3, 4 })
            {
                Create(); Populate(100); Submit(); yield return null;
                var calls = 0; var reason = ScrollCancelReason.None;
                list.ScrollToCell(80, animation: new ScrollAnimation(1), onFinished: result =>
                { Assert.That(result.Status, Is.EqualTo(ScrollStatus.Canceled)); reason = result.CancelReason; calls++; });
                if (kind == 0) list.enabled = false;
                else if (kind == 1) UnityEngine.Object.Destroy(list.gameObject);
                else if (kind == 2) scroll.viewport.sizeDelta = new Vector2(0, 200);
                else if (kind == 3) scroll.viewport.sizeDelta = new Vector2(300, 0);
                else { list.CancelAnimation(); UnityEngine.Object.Destroy(list.gameObject); }
                yield return null; yield return null;
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(reason, Is.EqualTo(kind < 2 ? ScrollCancelReason.Disabled : kind < 4 ? ScrollCancelReason.ViewportUnavailable : ScrollCancelReason.ExplicitCancel));
                if (list != null) { list.enabled = true; scroll.viewport.sizeDelta = new Vector2(300, 200); }
                yield return null; Assert.That(calls, Is.EqualTo(1)); Cleanup();
            }
            Create(); Populate(100); Submit(); yield return null; list.gameObject.SetActive(false);
            var destroyed = 0;
            list.ScrollToOffset(500, onFinished: result => { AssertCanceled(result, ScrollCancelReason.Destroyed); destroyed++; });
            UnityEngine.Object.Destroy(list.gameObject); yield return null; Assert.That(destroyed, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator FinishedCallbacksCanStartRequestsAndExceptionsDoNotCorruptState()
        {
            Create(); Populate(100); Submit(); yield return null;
            var results = new List<int>();
            list.ScrollToCell(50, onFinished: result =>
            {
                AssertCompleted(result); results.Add(1);
                list.ScrollToCell(70, animation: new ScrollAnimation(.05f), offsetPixels: -20,
                    onFinished: next => { AssertCompleted(next); results.Add(2); });
            });
            Assert.That(list.IsAnimating, Is.True); yield return UntilFinished(() => results.Count == 2);
            Assert.That(list.Offset, Is.EqualTo(2820).Within(1));
            list.ScrollToCell(80, animation: new ScrollAnimation(10), onFinished: result =>
            {
                AssertCanceled(result, ScrollCancelReason.Replaced);
                list.ScrollToCell(20, onFinished: next => { AssertCompleted(next); results.Add(3); });
            });
            list.ScrollToCell(40, animation: new ScrollAnimation(10), onFinished: result => { AssertCanceled(result, ScrollCancelReason.Replaced); results.Add(4); });
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 3 }, results); Assert.That(list.Offset, Is.EqualTo(800).Within(1));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException: scroll callback probe"));
            list.ScrollToCell(10, onFinished: result => { list.ScrollToCell(60, animation: new ScrollAnimation(.05f)); throw new InvalidOperationException("scroll callback probe"); });
            Assert.That(list.IsAnimating, Is.True); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(list.Offset, Is.EqualTo(2400).Within(1)); list.ScrollToCell(5); Assert.That(list.Offset, Is.EqualTo(200).Within(1));
        }
        [UnityTest]
        public IEnumerator CellCancellationNotificationRunsAfterCellCallbackGuards()
        {
            Create(registerBinding: false); Populate(100);
            var inside = false; var armed = false; var calls = 0;
            list.RegisterCellBinding<Image>((cell, index, context) =>
            {
                if (!armed) return;
                inside = true; armed = false; list.CancelAnimation(); Assert.That(calls, Is.Zero); inside = false;
            });
            Submit(); yield return null; armed = true;
            list.ScrollToCell(80, animation: new ScrollAnimation(10), onFinished: result =>
            { AssertCanceled(result, ScrollCancelReason.ExplicitCancel); Assert.That(inside, Is.False); calls++; list.RefillCells(new RefillOptions(20)); });
            list.RefreshCells(); Assert.That(calls, Is.EqualTo(1)); Assert.That(list.Offset, Is.EqualTo(800).Within(1));
        }
        [UnityTest]
        public IEnumerator ClickCancellationDefersNotificationWithoutRestrictingClickDataUpdates()
        {
            Create(); Populate(100); Submit(); yield return null;
            var inside = false; var calls = 0;
            list.CellClicked += (cell, context) =>
            {
                inside = true; list.CancelAnimation(); Assert.That(calls, Is.Zero);
                list.RefillCells(new RefillOptions(20)); inside = false;
            };
            list.ScrollToCell(80, animation: new ScrollAnimation(10), onFinished: result =>
            { AssertCanceled(result, ScrollCancelReason.ExplicitCancel); Assert.That(inside, Is.False); calls++; list.ScrollToCell(30); });
            var events = new GameObject("ClickRequestEvents", typeof(EventSystem));
            try { list.GetVisibleCell(0).OnPointerClick(new PointerEventData(events.GetComponent<EventSystem>())); }
            finally { UnityEngine.Object.DestroyImmediate(events); }
            Assert.That(calls, Is.EqualTo(1)); Assert.That(list.Offset, Is.EqualTo(1200).Within(1));
        }
        [UnityTest]
        public IEnumerator DynamicImmediateRemeasuresBeforeCompletionAndDisableCancelsConvergence()
        {
            Create(dynamic: true); Populate(100);
            list.RegisterCellBinding<LayoutElement>((cell, index, context) => cell.preferredHeight = 80); Submit();
            yield return null; yield return null;
            var calls = 0;
            list.ScrollToCell(50, ScrollAlignment.End, offsetPixels: -20, onFinished: result =>
            {
                AssertCompleted(result); calls++;
                var target = list.GetVisibleCell(50).RectTransform;
                Assert.That(-target.anchoredPosition.y - list.Offset, Is.EqualTo(60).Within(1));
            });
            var element = list.GetVisibleCell(50).GetComponent<LayoutElement>(); element.preferredHeight = 120;
            list.InvalidateCellSize(50); Assert.That(calls, Is.Zero);
            yield return UntilFinished(() => calls > 0); Assert.That(calls, Is.EqualTo(1));
            list.ScrollToCell(10, onFinished: result => { AssertCanceled(result, ScrollCancelReason.Disabled); calls++; });
            Assert.That(list.IsAnimating, Is.False); list.enabled = false; Assert.That(calls, Is.EqualTo(2));
            list.enabled = true; yield return null; yield return null; Assert.That(calls, Is.EqualTo(2));
        }
        [UnityTest]
        public IEnumerator DynamicRequestKeepsOffsetThroughMeasurementRefreshResizeAndDoesNotRenotify()
        {
            foreach (var mode in new[] { LoopLayout.Vertical, LoopLayout.Horizontal })
            foreach (ScrollAlignment alignment in Enum.GetValues(typeof(ScrollAlignment)))
            foreach (var pixels in new[] { -25f, 25f })
            {
                Create(mode, dynamic: true); Populate(100);
                var preferred = list.IsVertical ? 80f : 140f;
                list.RegisterCellBinding<LayoutElement>((cell, index, context) => { cell.preferredHeight = preferred; cell.preferredWidth = preferred; });
                Submit(); yield return null; yield return null;
                var calls = 0;
                list.ScrollToCell(50, alignment, new ScrollAnimation(.15f), pixels, result =>
                {
                    AssertCompleted(result); calls++;
                    var target = list.GetVisibleCell(50).RectTransform;
                    var length = list.ViewportLength;
                    var line = alignment == ScrollAlignment.Start ? 0 : (length - preferred) * (alignment == ScrollAlignment.Center ? .5f : 1);
                    var local = list.IsVertical ? -target.anchoredPosition.y - list.Offset : target.anchoredPosition.x - list.Offset;
                    Assert.That(local, Is.EqualTo(line + pixels).Within(1));
                });
                yield return null; list.RefreshCells();
                var cell50 = list.GetVisibleCell(50);
                if (cell50 != null) { preferred += 20; cell50.GetComponent<LayoutElement>().preferredHeight = preferred; cell50.GetComponent<LayoutElement>().preferredWidth = preferred; list.InvalidateCellSize(50); }
                scroll.viewport.sizeDelta = new Vector2(280, 180);
                yield return UntilFinished(() => calls > 0); Assert.That(calls, Is.EqualTo(1));
                var targetAfter = list.GetVisibleCell(50); targetAfter.GetComponent<LayoutElement>().preferredHeight += 20;
                targetAfter.GetComponent<LayoutElement>().preferredWidth += 20; list.InvalidateCellSize(50);
                yield return null; yield return null; Assert.That(calls, Is.EqualTo(1)); Cleanup();
            }
        }
        [UnityTest]
        public IEnumerator DynamicImmediateNotifiesAfterStableVisibleLayout()
        {
            Create(dynamic: true); Populate(100);
            list.RegisterCellBinding<LayoutElement>((cell, index, context) => cell.preferredHeight = 80); Submit();
            var calls = 0;
            list.ScrollToCell(50, ScrollAlignment.Center, offsetPixels: 30, onFinished: result =>
            { AssertCompleted(result); calls++; Assert.That(list.GetVisibleCell(50).RectTransform.anchoredPosition.y + list.Offset, Is.EqualTo(-90).Within(1)); });
            Assert.That(calls, Is.Zero); Assert.That(list.IsAnimating, Is.False);
            yield return UntilFinished(() => calls > 0); Assert.That(calls, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator CarouselOffsetRequestCompletesBeforeCoordinateRecenterAndStillSnaps()
        {
            Create(LoopLayout.Horizontal); var carousel = list.gameObject.AddComponent<LoopCarouselController>();
            carousel.Configure(300, 0, .05f); Populate(5); Submit(); yield return null; yield return null;
            var completed = 0;
            list.ScrollToOffset(-300, new ScrollAnimation(.05f), result => { AssertCompleted(result); Assert.That(list.Offset, Is.EqualTo(-300).Within(1)); completed++; });
            yield return UntilFinished(() => completed > 0); Assert.That(completed, Is.EqualTo(1));
            carousel.Next(); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Mathf.Abs(list.Offset), Is.LessThan(1500));
            var events = new GameObject("CarouselRequestEvents", typeof(EventSystem));
            try
            {
                var canceled = 0; var pointer = new PointerEventData(events.GetComponent<EventSystem>());
                list.ScrollToOffset(12000, new ScrollAnimation(1), result => { AssertCanceled(result, ScrollCancelReason.DragStarted); canceled++; });
                list.OnBeginDrag(pointer); list.OnEndDrag(pointer); scroll.StopMovement();
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(canceled, Is.EqualTo(1)); Assert.That(completed, Is.EqualTo(1)); Assert.That(list.IsAnimating, Is.False);
                Assert.That(list.Offset, Is.EqualTo(carousel.CurrentPage * 300).Within(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(events); }
        }
        [UnityTest]
        public IEnumerator WarmRegisteredScrollingSixtySecondsAllocatesNoPluginMemory()
        {
            Create(prewarm: 50); Populate(100000); Submit();
            yield return null;
            for (var i = 0; i < 100; i++) list.ScrollToCell(i * 500);
            yield return null; var created = list.CreatedCellCount;
            var until = Time.realtimeSinceStartup + 60; var step = 0; long totalAllocated = 0;
            using (var probe = new GcAllocationProbe())
            {
            while (Time.realtimeSinceStartup < until)
            {
                probe.Begin();
                list.ScrollToCell((step++ * 37) % 99000);
                totalAllocated += probe.End();
                yield return null;
            }
            }
            Assert.That(totalAllocated, Is.Zero, "同步 ScrollToCell/Reconcile 的当前线程内部分配");
            Assert.That(list.CreatedCellCount, Is.EqualTo(created));
            Debug.Log($"SleepyLoopScroll 60s synchronous benchmark: calls={step}, managedBytes={totalAllocated}, created={created}");
        }
    }
}
