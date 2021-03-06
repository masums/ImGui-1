﻿using System.Collections.Generic;
using ImGui.Common.Primitive;
using System.Diagnostics;

namespace ImGui.Layout
{
    internal partial class StackLayout
    {
        static ObjectPool<LayoutEntry> EntryPool = new ObjectPool<LayoutEntry>(1024);
        static ObjectPool<LayoutGroup> GroupPool = new ObjectPool<LayoutGroup>(1024);

        public bool dirty;

        private readonly Stack<LayoutGroup> stackA = new Stack<LayoutGroup>();
        private readonly Stack<LayoutGroup> stackB = new Stack<LayoutGroup>();

        private Stack<LayoutGroup> WritingStack { get; set; }
        public Stack<LayoutGroup> ReadingStack { get; private set; }

        public LayoutGroup TopGroup => this.ReadingStack.Peek();

        private void SwapStack()
        {
            var t = this.ReadingStack;
            this.ReadingStack = this.WritingStack;
            this.WritingStack = t;
        }

        private LayoutGroup CreateRootGroup(int rootId, Size size)
        {
            var rootGroup = new LayoutGroup();
            rootGroup.Init(rootId, true, null);
            rootGroup.ContentWidth = size.Width;
            rootGroup.ContentHeight = size.Height;
            rootGroup.HorizontalStretchFactor = 1;
            //rootGroup.VerticalStretchFactor = 1;
            return rootGroup;
        }

        public StackLayout(int rootId, Size size)
        {
            var rootGroup = CreateRootGroup(rootId, size);
            this.stackA.Push(rootGroup);
            this.WritingStack = this.stackA;

            var rootGroupX = CreateRootGroup(rootId, size);
            this.stackB.Push(rootGroupX);
            this.ReadingStack = this.stackB;
        }

        public Rect GetRect(int id, Size contentSize, LayoutOptions? options = null, string str_id = null)
        {
            // FIXME This should only be checked if the rect's width or height is not stretched.
            //if (contentSize.Height < 1 || contentSize.Width < 1)
            //{
            //    throw new ArgumentOutOfRangeException(nameof(contentSize), "Content size is too small.");
            //}

            // build entry for next frame
            {
                var entry = EntryPool.Get();
                entry.Init(id, contentSize, options);
                entry.StrId = str_id;

                //var entry = new LayoutEntry(id, contentSize);
                this.WritingStack.Peek().Add(entry);
            }

            // read from built group
            {
                var group = this.ReadingStack.Peek();
                var entry = group.GetEntry(id);
                if(entry == null)
                {
                    return DummyRect;//dummy
                }
                var rect = entry.Rect;
                return rect;
            }
        }

        public void BeginLayoutGroup(int id, bool isVertical, LayoutOptions? options = null, string str_id = null)
        {
            // build group for next frame
            {
                var group = GroupPool.Get();
                group.Init(id, isVertical, options);
                group.StrId = str_id;
                this.WritingStack.Peek().Add(group);
                this.WritingStack.Push(group);
            }

            // read from built group
            {
                var parentGroup = this.ReadingStack.Peek();
                LayoutGroup group = null;
                if (parentGroup != null)
                {
                    group = parentGroup.GetEntry(id) as LayoutGroup;
#if DrawGroup
                    if(group!= null)
                    {
                        var window = Form.current.uiContext.WindowManager.CurrentWindow;
                        var rect = window.GetRect(group.Rect);
                        window.DrawList.AddRectFilled(rect, group.IsVertical ? Color.Argb(30, 255, 0, 0) : Color.Argb(30, 0, 0, 255));
                    }
#endif
                }
                if(group == null)// this happens when new group is added in previous frame
                {
                    group = DummyGroup;//dummy (HACK added to reading stack to forbid NRE)
                }
                this.ReadingStack.Push(group);
            }
        }

        public void EndLayoutGroup()
        {
            this.WritingStack.Pop();
            this.ReadingStack.Pop();
        }

        void PutBackEntries(LayoutGroup group)
        {
            foreach (var entry in group.Entries)
            {
                var childGroup = entry as LayoutGroup;
                if (childGroup == null)
                {
                    EntryPool.Put(entry);
                }
                else
                {
                    PutBackEntries(childGroup);
                    if (childGroup == StackLayout.DummyGroup) continue;
                    GroupPool.Put(childGroup);
                }
            }

            group.Entries.Clear();
        }

        public void Begin()
        {
        }

        /// <summary>
        /// Calculate positions and sizes of every LayoutGroup and LayoutEntry
        /// </summary>
        public void Layout()
        {
            this.WritingStack.Peek().CalcWidth(TopGroup.ContentWidth);
            this.WritingStack.Peek().CalcHeight(TopGroup.ContentHeight);
            this.WritingStack.Peek().SetX(0);
            this.WritingStack.Peek().SetY(0);

            this.SwapStack();

            Debug.Assert(this.WritingStack.Count == 1);
            Debug.Assert(this.ReadingStack.Count == 1);
            var rootGroup = this.WritingStack.Peek();
            PutBackEntries(rootGroup);//remove all children of root group
        }

        public void SetRootSize(Size size)
        {
            {
                var rootGroup = this.ReadingStack.Peek();
                rootGroup.ContentWidth = size.Width;
                rootGroup.ContentHeight = size.Height;
            }
            {
                var rootGroup = this.WritingStack.Peek();
                rootGroup.ContentWidth = size.Width;
                rootGroup.ContentHeight = size.Height;
            }
        }

        public static Rect DummyRect = new Rect(1, 1);
        public static LayoutGroup DummyGroup = CreateDummyGroup();
        private static LayoutGroup CreateDummyGroup()
        {
            var group = new LayoutGroup();
            group.Init(-9999, true, null);
            return group;
        }
    }
}
