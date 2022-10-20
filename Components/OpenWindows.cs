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
        /// List of all the open windows
        /// </summary>
        private readonly List<Window> windows = new List<Window>();

        /// <summary>
        /// An instance of the class OpenWindows
        /// </summary>
        private static OpenWindows? instance;

        /// <summary>
        /// Gets the list of all open windows
        /// </summary>
        public List<Window> Windows
        {
            get { return new List<Window>(windows.Where(w => !w.IsFlowWindow)); }
        }

        public Window? FlowWindow => windows.Find(w => w.IsFlowWindow);

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
        /// Updates the list of open windows
        /// </summary>
        public void UpdateOpenWindowsList()
        {
            windows.Clear();
            EnumWindowsProc callbackptr = WindowEnumerationCallBack;
            _ = NativeMethods.EnumWindows(callbackptr, 0);
        }

        private static string flowLauncherExe = "Flow.Launcher.exe";

        /// <summary>
        /// Call back method for window enumeration
        /// </summary>
        /// <param name="hwnd">The handle to the current window being enumerated</param>
        /// <param name="lParam">Value being passed from the caller (we don't use this but might come in handy
        /// in the future</param>
        /// <returns>true to make sure to continue enumeration</returns>
        public bool WindowEnumerationCallBack(IntPtr hwnd, IntPtr lParam)
        {
            Window newWindow = new Window(hwnd);

            if (newWindow.Process.Name == flowLauncherExe && newWindow.IsWindow)
            {
                newWindow.IsFlowWindow = true;
                windows.Add(newWindow);
            }

            if (newWindow.IsWindow && newWindow.Visible && newWindow.IsOwner
                && (!newWindow.IsToolWindow || newWindow.IsAppWindow) && !newWindow.TaskListDeleted
                && (SearchWindowsAcrossAllVDesktop || (newWindow.Desktop.IsVisible || Main.VirtualDesktopHelperInstance.GetDesktopCount() < 2))
                && newWindow.ClassName != "Windows.UI.Core.CoreWindow"
                && newWindow.Process.Name != flowLauncherExe
                && (!newWindow.IsCloaked ||
                    newWindow.GetWindowCloakState() ==
                    Window.WindowCloakState.OtherDesktop)
                // To hide (not add) preloaded uwp app windows that are invisible to the user and other cloaked windows,
                // we check the cloak state.
               )
            {
                windows.Add(newWindow);
            }

            return true;
        }
    }
}