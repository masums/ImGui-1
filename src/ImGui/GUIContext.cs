﻿using System;
using System.Collections.Generic;

namespace ImGui
{
    internal class GUIContext
    {

        public bool LogEnabled = true;

        // fps
        public long Time;
        public long FrameCount = 0;
        public long lastFPSUpdateTime;
        public int fps;

        public readonly List<Window> Windows = new List<Window>();
        public Window CurrentWindow;
        public readonly Stack<Window> CurrentWindowStack = new Stack<Window>();

        private int hoverId;
        private int activeId;
        private bool activeIdIsAlive;
        private bool activeIdIsJustActivated;

        private int hoverIdPreviousFrame;
        private int activeIdPreviousFrame;


        //-----

        public const int None = 0;

        public int HoverId
        {
            get { return hoverId; }
            set { hoverId = value; }
        }

        public int ActiveId
        {
            get { return activeId; }
            set { activeId = value; }
        }

        public bool ActiveIdIsAlive
        {
            get { return activeIdIsAlive; }
            set { activeIdIsAlive = value; }
        }

        public int ActiveIdPreviousFrame
        {
            get { return activeIdPreviousFrame; }
            set { activeIdPreviousFrame = value; }
        }

        public bool ActiveIdIsJustActivated
        {
            get { return activeIdIsJustActivated; }
            set { activeIdIsJustActivated = value; }
        }

        public int HoverIdPreviousFrame
        {
            get { return hoverIdPreviousFrame; }
            set { hoverIdPreviousFrame = value; }
        }

        public Window HoveredWindow { get; internal set; }
        public Window MovedWindow { get; internal set; }
        public Window HoveredRootWindow { get; internal set; }
        public int MovedWindowMoveId { get; internal set; }
        public Window FocusedWindow { get; private set; }
        public Window ActiveIdWindow { get; private set; }
        public bool ActiveIdAllowOverlap { get; private set; }
        public Vector ActiveIdClickOffset { get; internal set; }
        public bool HoveredIdAllowOverlap { get; internal set; }
        public long DeltaTime { get; internal set; }
        public int HoveredIdPreviousFrame { get; internal set; }

        public void SetHoverId(int id)
        {
            HoverId = id;
        }

        public void SetActiveId(int id)
        {
            ActiveId = id;
            ActiveIdIsJustActivated = true;

        }

        public void KeepAliveId(int id)
        {
            if (ActiveId == id)
                ActiveIdIsAlive = true;
        }


        public bool IsMouseHoveringRect(Rect rect, bool clip = true)
        {
            var g = this;
            Window window = CurrentWindow;

            //// Clip
            //Rect rect_clipped = rect;
            //if (clip)
            //    rect_clipped.Intersect(window.ClipRect);

            return rect.Contains(Input.Mouse.MousePos);
        }

        public bool IsMouseHoveringRect(Point r_min, Point r_max, bool clip = true)
        {
            Rect rect = new Rect(r_min, r_max);
            return IsMouseHoveringRect(rect, clip);
        }

        public Window FindWindowByName(string name)
        {
            var g = this;
            for (int i = 0; i < g.Windows.Count; i++)
            {
                if (g.Windows[i].ID == name.GetHashCode())
                {
                    return g.Windows[i];
                }
            }
            return null;
        }

        public Window FindHoveredWindow(Point pos, bool excluding_childs)
        {
            var g = this;
            for (int i = g.Windows.Count - 1; i >= 0; i--)
            {
                Window window = g.Windows[i];
                if (!window.Active)
                    continue;
                if (excluding_childs && (window.Flags & WindowFlags.ChildWindow) != 0)
                    continue;

                if (window.WindowClippedRect.Contains(pos))
                    return window;
            }
            return null;
        }

        public bool IsHovered(Rect bb, int id, bool flatten_childs)
        {
            GUIContext g = this;
            if (g.HoverId == 0 || g.HoverId == id || g.HoveredIdAllowOverlap)
            {
                Window window = g.CurrentWindow;
                if (g.HoveredWindow == window || (flatten_childs && g.HoveredRootWindow == window.RootWindow))
                    if ((g.ActiveId == 0 || g.ActiveId == id || g.ActiveIdAllowOverlap) && IsMouseHoveringRect(bb.Min, bb.Max))
                        if (IsWindowContentHoverable(g.HoveredRootWindow))
                            return true;
            }
            return false;
        }

        public bool IsWindowContentHoverable(Window window)
        {
            // An active popup disable hovering on other windows (apart from its own children)
            GUIContext g = this;
            Window focused_window = g.FocusedWindow;
            if (focused_window != null)
            {
                Window focused_root_window = focused_window.RootWindow;
                if (focused_root_window!=null)
                {
                    if (focused_root_window.Flags.HasFlag(WindowFlags.Popup) && focused_root_window.WasActive && focused_root_window != window.RootWindow)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsMouseLeftButtonClicked(bool repeat)
        {
            GUIContext g = this;
            long t = Input.Mouse.LeftButtonDownDuration;
            if (t == 0.0f)
                return true;

            if (repeat && t > Input.KeyRepeatDelay)
            {
                double delay = Input.KeyRepeatDelay, rate = Input.KeyRepeatRate;
                if (
                    ((t - delay)%rate) > rate * 0.5f
                    !=
                    ((t - delay - g.DeltaTime)% rate) > rate * 0.5f
                    )
                    return true;
            }

            return false;
        }

        public void KeepAliveID(int id)
        {
            if (this.ActiveId == id)
                this.ActiveIdIsAlive = true;
        }

        public void SetActiveID(int id, Window window = null)
        {
            var g = this;
            g.ActiveId = id;
            g.ActiveIdAllowOverlap = false;
            g.ActiveIdIsJustActivated = true;
            g.ActiveIdWindow = window;
        }

        public void FocusWindow(Window window)
        {
            var g = this;

            // Always mark the window we passed as focused. This is used for keyboard interactions such as tabbing.
            g.FocusedWindow = window;

            // Passing NULL allow to disable keyboard focus
            if (window == null) return;

            // And move its root window to the top of the pile
            if (window.RootWindow != null)
            {
                window = window.RootWindow;
            }

            // Steal focus on active widgets
            if (this.ActiveId != 0 && (g.ActiveIdWindow!=null) && g.ActiveIdWindow.RootWindow != window)
                    SetActiveID(0);
        }
    }
}