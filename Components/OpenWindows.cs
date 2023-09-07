// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code forked from Betsegaw Tadele's https://github.com/betsegaw/windowwalker/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Class that represents the state of the desktops windows
    /// </summary>
    internal class OpenWindows
    {
        /// <summary>
        /// List of all the open _windows
        /// </summary>
        private readonly List<Window> windows = new List<Window>();

        /// <summary>
        /// An instance of the class OpenWindows
        /// </summary>
        private static OpenWindows? instance;

        /// <summary>
        /// Gets the list of all open _windows
        /// </summary>
        public IReadOnlyList<Window> Windows => windows;

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
            FlowWindow = null;
            EnumWindowsProc callbackptr = FlowWindowCallback;
            _ = NativeMethods.EnumWindows(callbackptr, 0);
        }

        /// <summary>
        /// Updates the list of open _windows
        /// </summary>
        public void UpdateOpenWindowsList()
        {
            FlowWindow = null;
            windows.Clear();
            EnumWindowsProc callbackptr = WindowEnumerationCallBack;
            _ = NativeMethods.EnumWindows(callbackptr, 0);
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
        /// Call back method for window enumeration
        /// </summary>
        /// <param name="hwnd">The handle to the current window being enumerated</param>
        /// <param name="lParam">Value being passed from the caller (we don't use this but might come in handy
        /// in the future</param>
        /// <returns>true to make sure to continue enumeration</returns>
        public bool WindowEnumerationCallBack(IntPtr hwnd, IntPtr lParam)
        {
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
                && (!window.IsCloaked ||
                    window.GetWindowCloakState() ==
                    Window.WindowCloakState.OtherDesktop)
                // To hide (not add) preloaded uwp app _windows that are invisible to the user and other cloaked _windows,
                // we check the cloak state.
               )
            {
                windows.Add(window);
            }

            return true;
        }
    }
}