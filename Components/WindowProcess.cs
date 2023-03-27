// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Flow.Launcher.Plugin.SharedCommands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Flow.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Represents the process data of an open window. This class is used in the process cache and for the process object of the open window
    /// </summary>
    internal class WindowProcess
    {
        /// <summary>
        /// Maximum size of a file name
        /// </summary>
        private const int MaximumFileNameLength = 1000;

        /// <summary>
        /// Gets the id of the process
        /// </summary>
        internal uint ProcessID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the id of the thread
        /// </summary>
        internal uint ThreadID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the process
        /// </summary>
        internal string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the image of the process
        /// </summary>
        internal string Image
        {
            get;
            private set;
        } = String.Empty;

        /// <summary>
        /// Gets a value indicating whether the window belongs to an 'Universal Windows Platform (UWP)' process
        /// </summary>
        internal bool IsUwpApp { get; }

        /// <summary>
        /// Gets a value indicating whether this is the shell process or not
        /// The shell process (like explorer.exe) hosts parts of the user interface (like taskbar, start menu, ...)
        /// </summary>
        internal bool IsShellProcess
        {
            get
            {
                IntPtr hShellWindow = NativeMethods.GetShellWindow();

                return GetProcessIDFromWindowHandle(hShellWindow) == ProcessID;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the process exists on the machine
        /// </summary>
        internal bool DoesExist
        {
            get
            {
                try
                {
                    var p = Process.GetProcessById((int)ProcessID);
                    p.Dispose();

                    return true;
                }
                catch (InvalidOperationException)
                {
                    // Thrown when process not exist.
                    return false;
                }
                catch (ArgumentException)
                {
                    // Thrown when process not exist.
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether full access to the process is denied or not
        /// </summary>
        internal bool IsFullAccessDenied
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowProcess"/> class.
        /// </summary>
        /// <param name="pid">New process id.</param>
        /// <param name="tid">New thread id.</param>
        /// <param name="name">New process name.</param>
        /// <param name="image">New process image.</param>
        internal WindowProcess(uint pid, uint tid, string name, string image)
        {
            UpdateProcessInfo(pid, tid, name, image);
            IsUwpApp = Name!.ToUpperInvariant().Equals("APPLICATIONFRAMEHOST.EXE", StringComparison.Ordinal);
        }

        /// <summary>
        /// Updates the process information of the <see cref="WindowProcess"/> instance.
        /// </summary>
        /// <param name="pid">New process id.</param>
        /// <param name="tid">New thread id.</param>
        /// <param name="name">New process name.</param>
        /// <param name="image">New process image.</param>
        internal void UpdateProcessInfo(uint pid, uint tid, string name, string image)
        {
            // TODO: Add verification as to whether the process id and thread id is valid
            ProcessID = pid;
            ThreadID = tid;
            Name = name;
            Image = image;

            // Process can be elevated only if process id is not 0 (Dummy value on error)
            IsFullAccessDenied = pid != 0 && TestProcessAccessUsingAllAccessFlag(pid);
        }

        /// <summary>
        /// Gets the process ID for the window handle
        /// </summary>
        /// <param name="hwnd">The handle to the window</param>
        /// <returns>The process ID</returns>
        internal static uint GetProcessIDFromWindowHandle(IntPtr hwnd)
        {
            _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);

            return processId;
        }

        /// <summary>
        /// Gets the thread ID for the window handle
        /// </summary>
        /// <param name="hwnd">The handle to the window</param>
        /// <returns>The thread ID</returns>
        internal static uint GetThreadIDFromWindowHandle(IntPtr hwnd)
        {
            uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);

            return threadId;
        }

        /// <summary>
        /// Gets the process name for the process ID
        /// </summary>
        /// <param name="pid">The id of the process/param>
        /// <returns>A string representing the process name or an empty string if the function fails</returns>
        internal static (string, string) GetProcessNameAndImageFromProcessID(uint pid)
        {
            var processHandle = NativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, true, (int)pid);
            Span<char> buffer = stackalloc char[MaximumFileNameLength];
            uint numCharRead;
            var processName = string.Empty;
            var processImage = string.Empty;
            var capacity = MaximumFileNameLength;

            if (NativeMethods.QueryFullProcessImageName(processHandle, 0, buffer, ref capacity))
                processImage = buffer[..capacity].ToString();

            
            if ((numCharRead = NativeMethods.GetProcessImageFileName(processHandle, buffer, MaximumFileNameLength)) != 0)
                processName = buffer[..(int)numCharRead].ToString().Split('\\').Reverse().ToArray()[0];

            _ = CloseHandleIfNotNull(processHandle);

            return (processName, processImage);
        }

        /// <summary>
        /// Kills the process by it's id. If permissions are required, they will be requested.
        /// </summary>
        /// <param name="killProcessTree">Kill process and sub processes.</param>
        internal void KillThisProcess(bool killProcessTree)
        {
            if (IsFullAccessDenied)
            {
                var killTree = killProcessTree ? "/t" : string.Empty;
                var processInfo = new ProcessStartInfo()
                {
                    FileName = "taskkill.exe",
                    ArgumentList =
                    {
                        "/pid",
                        ProcessID.ToString(),
                        "/f",
                        killTree
                    },
                    Verb = "runas",
                    CreateNoWindow = true
                };

                ShellCommand.Execute(processInfo);
            }
            else
            {
                Process.GetProcessById((int)ProcessID).Kill(killProcessTree);
            }
        }

        /// <summary>
        /// Validate that the handle is not null and close it.
        /// </summary>
        /// <param name="handle">Handle to close.</param>
        /// <returns>Zero if native method fails and nonzero if the native method succeeds.</returns>
        public static bool CloseHandleIfNotNull(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                // Return true if there is nothing to close.
                return true;
            }

            return NativeMethods.CloseHandle(handle);
        }

        /// <summary>
        /// Gets a boolean value indicating whether the access to a process using the AllAccess flag is denied or not.
        /// </summary>
        /// <param name="pid">The process ID of the process</param>
        /// <returns>True if denied and false if not.</returns>
        private static bool TestProcessAccessUsingAllAccessFlag(uint pid)
        {
            var processHandle = NativeMethods.OpenProcess(ProcessAccessFlags.AllAccess, true, (int)pid);

            if (Marshal.GetLastWin32Error() == 5)
            {
                // Error 5 = ERROR_ACCESS_DENIED
                _ = CloseHandleIfNotNull(processHandle);

                return true;
            }

            _ = CloseHandleIfNotNull(processHandle);

            return false;
        }
    }
}