// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code forked from Betsegaw Tadele's https://github.com/betsegaw/windowwalker/

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Win32.SafeHandles;
using static Flow.Plugin.WindowWalker.Main;

namespace Flow.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Represents a specific open window
    /// </summary>
    public class Window
    {
        /// <summary>
        /// The handle to the window
        /// </summary>
        private readonly HWND hwnd;

        /// <summary>
        /// A static cache for the process data of all known windows
        /// that we don't have to query the data every time
        /// </summary>
        private static readonly Dictionary<IntPtr, WindowProcess> _handlesToProcessCache = new Dictionary<IntPtr, WindowProcess>();

        /// <summary>
        /// Gets the title of the window (the string displayed at the top of the window)
        /// </summary>
        internal string Title
        {
            get
            {
                var sizeOfTitle = PInvoke.GetWindowTextLength(hwnd);
                if (sizeOfTitle++ <= 0) return string.Empty;
                
                Span<char> titleBuffer = stackalloc char[sizeOfTitle];
                unsafe
                {
                    fixed (char* ptr = titleBuffer)
                    {
                        var numCharactersWritten = PInvoke.GetWindowText(hwnd, ptr, sizeOfTitle);
                        return numCharactersWritten == 0 ? string.Empty : titleBuffer[..numCharactersWritten].ToString();
                    }
                }

            }
        }

        /// <summary>
        /// Gets the handle to the window
        /// </summary>
        internal HWND Hwnd => hwnd;

        /// <summary>
        /// Gets the object of with the process information of the window
        /// </summary>
        internal WindowProcess Process { get; }

        /// <summary>
        /// Gets the object of with the desktop information of the window
        /// </summary>
        internal VDesktop Desktop { get; }

        internal bool IsFlowWindow { get; set; }

        /// <summary>
        /// Gets the name of the class for the window represented
        /// </summary>
        internal string ClassName => GetWindowClassName(Hwnd);

        /// <summary>
        /// Gets a value indicating whether the window is visible (might return false if it is a hidden IE tab)
        /// </summary>
        internal bool Visible => PInvoke.IsWindowVisible(Hwnd);

        /// <summary>
        /// Gets a value indicating whether the window is cloaked (true) or not (false).
        /// (A cloaked window is not visible to the user. But the window is still composed by DWM.)
        /// </summary>
        internal bool IsCloaked => GetWindowCloakState() != WindowCloakState.None;

        /// <summary>
        /// Gets a value indicating whether the specified window handle identifies an existing window.
        /// </summary>
        internal bool IsWindow => PInvoke.IsWindow(Hwnd);

        /// <summary>
        /// Gets a value indicating whether the window is a toolwindow
        /// </summary>
        internal bool IsToolWindow => (PInvoke.GetWindowLong(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE) &
                                       (uint)ExtendedWindowStyles.WS_EX_TOOLWINDOW) ==
                                      (uint)ExtendedWindowStyles.WS_EX_TOOLWINDOW;

        /// <summary>
        /// Gets a value indicating whether the window is an appwindow
        /// </summary>
        internal bool IsAppWindow => (PInvoke.GetWindowLong(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE) &
                                      (uint)ExtendedWindowStyles.WS_EX_APPWINDOW) ==
                                     (uint)ExtendedWindowStyles.WS_EX_APPWINDOW;

        /// <summary>
        /// Gets a value indicating whether the window has ITaskList_Deleted property
        /// </summary>
        internal bool TaskListDeleted
        {
            get
            {
                using var handle = PInvoke.GetProp(Hwnd, "ITaskList_Deleted");
                return handle.IsInvalid;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified windows is the owner (i.e. doesn't have an owner)
        /// </summary>
        internal bool IsOwner => PInvoke.GetWindow(Hwnd, GET_WINDOW_CMD.GW_OWNER) == IntPtr.Zero;

        /// <summary>
        /// Gets a value indicating whether the window is minimized
        /// </summary>
        internal bool Minimized => GetWindowSizeState() == WindowSizeState.Minimized;

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// Initializes a new Window representation
        /// </summary>
        /// <param name="hwnd">the handle to the window we are representing</param>
        internal Window(HWND hwnd)
        {
            // TODO: Add verification as to whether the window handle is valid
            this.hwnd = hwnd;
            Process = CreateWindowProcessInstance(hwnd);
            Desktop = VirtualDesktopHelperInstance.GetWindowDesktop(hwnd);
        }

        /// <summary>
        /// Switches desktop focus to the window
        /// </summary>
        internal void SwitchToWindow()
        {
            // The following block is necessary because
            // 1) There is a weird flashing behavior when trying
            //    to use ShowWindow for switching tabs in IE
            // 2) SetForegroundWindow fails on minimized windows
            // Using Ordinal since this is internal
            if (Process.Name.ToUpperInvariant().Equals("IEXPLORE.EXE", StringComparison.Ordinal) || !Minimized)
            {
                PInvoke.SetForegroundWindow(Hwnd);
            }
            else
            {
                if (!PInvoke.ShowWindow(Hwnd, SHOW_WINDOW_CMD.SW_RESTORE))
                {
                    // ShowWindow doesn't work if the process is running elevated: fallback to SendMessage
                    _ = PInvoke.SendMessage(Hwnd, Win32Constants.WM_SYSCOMMAND, Win32Constants.SC_RESTORE, default);
                }
            }

            PInvoke.FlashWindow(Hwnd, true);
        }

        /// <summary>
        /// Closes the window
        /// </summary>
        internal void CloseThisWindow(bool switchBeforeClose)
        {
            if (switchBeforeClose)
            {
                SwitchToWindow();
            }

            _ = PInvoke.SendMessage(Hwnd, Win32Constants.WM_SYSCOMMAND, Win32Constants.SC_CLOSE, default);
        }

        /// <summary>
        /// Converts the window name to string along with the process name
        /// </summary>
        /// <returns>The title of the window</returns>
        public override string ToString()
        {
            // Using CurrentCulture since this is user facing
            return Title + " (" + Process.Name.ToUpper(CultureInfo.CurrentCulture) + ")";
        }

        /// <summary>
        /// Returns what the window size is
        /// </summary>
        /// <returns>The state (minimized, maximized, etc..) of the window</returns>
        internal WindowSizeState GetWindowSizeState()
        {
            Windows.Win32.UI.WindowsAndMessaging.WINDOWPLACEMENT placement = default;
            PInvoke.GetWindowPlacement(Hwnd, ref placement);

            switch (placement.showCmd)
            {
                case SHOW_WINDOW_CMD.SW_NORMAL:
                    return WindowSizeState.Normal;
                case SHOW_WINDOW_CMD.SW_MINIMIZE:
                case SHOW_WINDOW_CMD.SW_SHOWMINIMIZED:
                    return WindowSizeState.Minimized;
                case SHOW_WINDOW_CMD.SW_MAXIMIZE: // No need for ShowMaximized here since its also of value 3
                    return WindowSizeState.Maximized;
                default:
                    // throw new Exception("Don't know how to handle window state = " + placement.ShowCmd);
                    return WindowSizeState.Unknown;
            }
        }

        /// <summary>
        /// Enum to simplify the state of the window
        /// </summary>
        internal enum WindowSizeState
        {
            Normal,
            Minimized,
            Maximized,
            Unknown,
        }

        /// <summary>
        /// Returns the window cloak state from DWM
        /// (A cloaked window is not visible to the user. But the window is still composed by DWM.)
        /// </summary>
        /// <returns>The state (none, app, ...) of the window</returns>
        internal unsafe WindowCloakState GetWindowCloakState()
        {
            int isCloakedState = 0;
            
            _ = PInvoke.DwmGetWindowAttribute(Hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &isCloakedState, sizeof(uint));

            return isCloakedState switch
            {
                (int)DwmWindowCloakStates.None => WindowCloakState.None,
                (int)DwmWindowCloakStates.CloakedApp => WindowCloakState.App,
                (int)DwmWindowCloakStates.CloakedShell => VirtualDesktopHelperInstance
                    .IsWindowCloakedByVirtualDesktopManager(hwnd, Desktop.Id)
                    ? WindowCloakState.OtherDesktop
                    : WindowCloakState.Shell,
                (int)DwmWindowCloakStates.CloakedInherited => WindowCloakState.Inherited,
                _ => WindowCloakState.Unknown
            };
        }

        /// <summary>
        /// Enum to simplify the cloak state of the window
        /// </summary>
        internal enum WindowCloakState
        {
            None,
            App,
            Shell,
            Inherited,
            OtherDesktop,
            Unknown,
        }

        /// <summary>
        /// Returns the class name of a window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>Class name</returns>
        private static string GetWindowClassName(HWND hwnd)
        {
            Span<char> windowClassName = stackalloc char[300];
            unsafe
            {
                fixed (char* ptr = windowClassName)
                {
                    var numCharactersWritten = PInvoke.GetClassName(hwnd, ptr, 300);
                    return numCharactersWritten == 0 ? string.Empty : windowClassName[..numCharactersWritten].ToString();
                }
            }
        }

        /// <summary>
        /// Gets an instance of <see cref="WindowProcess"/> form process cache or creates a new one. A new one will be added to the cache.
        /// </summary>
        /// <param name="hWindow">The handle to the window</param>
        /// <returns>A new Instance of type <see cref="WindowProcess"/></returns>
        private static WindowProcess CreateWindowProcessInstance(HWND hWindow)
        {
            lock (_handlesToProcessCache)
            {
                if (_handlesToProcessCache.Count > 7000)
                {
                    Debug.Print("Clearing Process Cache because it's size is " + _handlesToProcessCache.Count);
                    _handlesToProcessCache.Clear();
                }

                // Add window's process to cache if missing
                if (!_handlesToProcessCache.ContainsKey(hWindow))
                {
                    // Get process ID and name
                    var processId = WindowProcess.GetProcessIDFromWindowHandle(hWindow);
                    var threadId = WindowProcess.GetThreadIDFromWindowHandle(hWindow);
                    (var processName, var processImage) = WindowProcess.GetProcessNameAndImageFromProcessID(processId);

                    if (processName.Length != 0)
                    {
                        _handlesToProcessCache.Add(hWindow, new WindowProcess(processId, threadId, processName, processImage));
                    }
                    else
                    {
                        // For the dwm process we can not receive the name. This is no problem because the window isn't part of result list.
                        Context.API.LogDebug("Flow.Plugin.WindowWalker.Components.Window", $"Invalid process {processId} ({processName}) for window handle {hWindow}.");
                        _handlesToProcessCache.Add(hWindow, new WindowProcess(0, 0, string.Empty, String.Empty));
                    }
                }

                // Correct the process data if the window belongs to a uwp app hosted by 'ApplicationFrameHost.exe'
                // (This only works if the window isn't minimized. For minimized windows the required child window isn't assigned.)
                if (_handlesToProcessCache[hWindow].Name.ToUpperInvariant() == "APPLICATIONFRAMEHOST.EXE")
                {
                    new Task(() =>
                    { 
                        _ = PInvoke.EnumChildWindows(hWindow, (hwnd, _) =>
                        {
                            // Every uwp app main window has at least three child windows. Only the one we are interested in has a class starting with "Windows.UI.Core." and is assigned to the real app process.
                            // (The other ones have a class name that begins with the string "ApplicationFrame".)
                            if (!GetWindowClassName(hwnd)
                                    .StartsWith("Windows.UI.Core.", StringComparison.OrdinalIgnoreCase)) return true;
                            
                            var childProcessId = WindowProcess.GetProcessIDFromWindowHandle(hwnd);
                            var childThreadId = WindowProcess.GetThreadIDFromWindowHandle(hwnd);
                            var (childProcessName, childProcessImage) = WindowProcess.GetProcessNameAndImageFromProcessID(childProcessId);

                            // Update process info in cache
                            _handlesToProcessCache[hWindow].UpdateProcessInfo(childProcessId, childThreadId, childProcessName, childProcessImage);
                            return false;

                        }, 0);
                    }).Start();
                }

                return _handlesToProcessCache[hWindow];
            }
        }
    }
}
