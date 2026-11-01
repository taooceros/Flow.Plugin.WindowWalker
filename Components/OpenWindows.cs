// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code forked from Betsegaw Tadele's https://github.com/betsegaw/windowwalker/

using System;
using System.Collections.Generic;

namespace Flow.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Class that represents the state of the desktops windows
    /// </summary>
    internal class OpenWindows
    {
        /// <summary>
        /// An instance of the class OpenWindows
        /// </summary>
        private static OpenWindows? instance;

        private static readonly object _enumWindowsLock = new();

        private IReadOnlyList<Window> _windows = Array.Empty<Window>();

        /// <summary>
        /// Gets the list of all open _windows (thread-safe snapshot)
        /// </summary>
        public IReadOnlyList<Window> Windows
        {
            get
            {
                lock (_enumWindowsLock)
                {
                    return _windows;
                }
            }
        }

        public Window? FlowWindow { get; private set; }

        /// <summary>
        /// Gets an instance property of this class that makes sure that
        /// the first instance gets created and that all the requests
        /// end up at that one instance
        /// </summary>
        public static OpenWindows Instance => instance ??= new OpenWindows();

        public bool SearchWindowsAcrossAllVDesktop = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenWindows"/> class.
        /// Private constructor to make sure there is never
        /// more than one instance of this class
        /// </summary>
        private OpenWindows()
        {
        }

        /// <summary>
        /// Updates the reference to FlowWindow
        /// </summary>
        public void UpdateFlowWindow()
        {
            lock (_enumWindowsLock)
            {
                FlowWindow = null;
                EnumWindowsProc callbackptr = FlowWindowCallback;
                _ = NativeMethods.EnumWindows(callbackptr, 0);
            }
        }

        private static string flowLauncherExe = "Flow.Launcher.exe";

        /// <summary>
        /// Call back method for window enumeration, to find the Flow Window
        /// </summary>
        /// <param name="hwnd">The handle to the current window being enumerated</param>
        /// <param name="lParam">Value being passed from the caller (we don't use this but might come in handy
        /// in the future</param>
        /// <returns>true to make sure to continue enumeration</returns>
        public bool FlowWindowCallback(IntPtr hwnd, IntPtr lParam)
        {
            Window window = new Window(hwnd);

            if (window.Process.Name == flowLauncherExe && window.IsWindow)
            {
                window.IsFlowWindow = true;
                FlowWindow = window;
                return false; // Stop enumeration
            }

            return true;
        }

        /// <summary>
        /// Updates the list of open _windows in a thread-safe manner with HWND deduplication
        /// </summary>
        public void UpdateOpenWindowsList()
        {
            lock (_enumWindowsLock)
            {
                FlowWindow = null;
                var newWindows = new List<Window>();
                var seenHwnds = new HashSet<IntPtr>();

                bool Callback(IntPtr hwnd, IntPtr lParam)
                {
                    if (!seenHwnds.Add(hwnd))
                        return true;

                    Window window = new Window(hwnd);

                    if (window.Process.Name == flowLauncherExe && window.IsWindow)
                    {
                        window.IsFlowWindow = true;
                        FlowWindow = window;
                        return true;
                    }

                    if (window.IsWindow && window.Visible && window.IsOwner
                        && (!window.IsToolWindow || window.IsAppWindow) && !window.TaskListDeleted
                        && (SearchWindowsAcrossAllVDesktop || (window.Desktop.IsVisible || Main.VirtualDesktopHelperInstance.GetDesktopCount() < 2))
                        && window.ClassName != "Windows.UI.Core.CoreWindow"
                        && window.Process.Name != flowLauncherExe
                        && (Main.Settings.SearchCloakedWindows ||
                            !window.IsCloaked ||
                            window.GetWindowCloakState() ==
                            Window.WindowCloakState.OtherDesktop)
                        // To hide (not add) preloaded uwp app _windows that are invisible to the user and other cloaked _windows,
                        // we check the cloak state.
                       )
                    {
                        newWindows.Add(window);
                    }

                    return true;
                }

                _ = NativeMethods.EnumWindows(Callback, 0);
                _windows = newWindows;
            }
        }
    }
}
