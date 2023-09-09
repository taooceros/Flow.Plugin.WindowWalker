// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Flow.Plugin.WindowWalker.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Flow.Plugin.WindowWalker.Components.COM;
using IVirtualDesktopManager = Windows.Win32.UI.Shell.IVirtualDesktopManager;
using System.Windows.Controls;
using Windows.Win32.Graphics.Dwm;

namespace Flow.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Helper class to work with Virtual Desktops.
    /// This helper uses only public available and documented COM-Interfaces or informations from registry.
    /// </summary>
    /// <remarks>
    /// To use this helper you have to create an instance of it and access the method via the helper instance.
    /// We are only allowed to use public documented com interfaces.
    /// </remarks>
    /// <SeeAlso href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ivirtualdesktopmanager">Documentation of IVirtualDesktopManager interface</SeeAlso>
    /// <SeeAlso href="https://docs.microsoft.com/en-us/archive/blogs/winsdk/virtual-desktop-switching-in-windows-10">CSharp example code for IVirtualDesktopManager</SeeAlso>
    public class VirtualDesktopHelper
    {
        /// <summary>
        /// Are we running on Windows 11
        /// </summary>
        private static readonly bool _IsWindowsEleven = IsWindowsElevenOrLater();

        /// <summary>
        /// Instance of "Virtual Desktop Manager"
        /// </summary>
        private readonly IVirtualDesktopManager? _virtualDesktopManager;

        /// <summary>
        /// Internal settings to enable automatic update of desktop list.
        /// This will be off by default to avoid to many registry queries.
        /// </summary>
        private readonly bool _desktopListAutoUpdate;

        /// <summary>
        /// List of all available Virtual Desktop in their real order
        /// The order and list in the registry is always up to date
        /// </summary>
        private List<Guid> availableDesktops = new List<Guid>();

        /// <summary>
        /// Id of the current visible Desktop.
        /// </summary>
        private Guid currentDesktop;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualDesktopHelper"/> class.
        /// </summary>
        /// <param name="desktopListUpdate">Setting to configure if the list of available desktops should update automatically or only when calling <see cref="UpdateDesktopList"/>. Per default this is set to manual update (false) to have less registry queries.</param>
        public VirtualDesktopHelper(bool desktopListUpdate = false)
        {
            try
            {
                _virtualDesktopManager = (IVirtualDesktopManager)new CVirtualDesktopManager();
            }
            catch (COMException ex)
            {
                Log.Exception("Initialization of <VirtualDesktopHelper> failed: An exception was thrown when creating the instance of COM interface <IVirtualDesktopManager>.", ex, typeof(VirtualDesktopHelper));
                return;
            }

            _desktopListAutoUpdate = desktopListUpdate;
            UpdateDesktopList();
        }

        /// <summary>
        /// Gets a value indicating whether the Virtual Desktop Manager is initialized successfully
        /// </summary>
        public bool VirtualDesktopManagerInitialized
        {
            get { return _virtualDesktopManager != null; }
        }

        /// <summary>
        /// Method to update the list of Virtual Desktops from Registry
        /// The data in the registry are always up to date
        /// </summary>
        /// <remarks>If we can not read from registry, we set the list/guid to empty values.</remarks>
        public void UpdateDesktopList()
        {
            // Registry paths
            var userSessionId = Process.GetCurrentProcess().SessionId;
            var registrySessionVirtualDesktops = $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\SessionInfo\\{userSessionId}\\VirtualDesktops";
            var registryExplorerVirtualDesktops = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VirtualDesktops";

            // List of all desktops
            var allDeskValue = (byte[]?)Registry.CurrentUser.OpenSubKey(registryExplorerVirtualDesktops, false)?.GetValue("VirtualDesktopIDs", null);
            if (allDeskValue != null)
            {
                // We clear only, if we can read from registry. Otherwise we keep the existing values.
                availableDesktops.Clear();

                // Each guid has a length of 16 elements
                var numberOfDesktops = allDeskValue.Length / 16;
                for (var i = 0; i < numberOfDesktops; i++)
                {
                    var guidArray = new byte[16];
                    Array.ConstrainedCopy(allDeskValue, i * 16, guidArray, 0, 16);
                    availableDesktops.Add(new Guid(guidArray));
                }
            }
            else
            {
                Log.Debug("VirtualDesktopHelper.UpdateDesktopList() failed to read the list of existing desktops form registry.", typeof(VirtualDesktopHelper));
            }

            // Guid for current desktop
            var currentDeskSessionValue = Registry.CurrentUser.OpenSubKey(registrySessionVirtualDesktops, false)?.GetValue("CurrentVirtualDesktop", null); // Windows 10
            var currentDeskExplorerValue = Registry.CurrentUser.OpenSubKey(registryExplorerVirtualDesktops, false)?.GetValue("CurrentVirtualDesktop", null); // Windows 11
            var currentDeskValue = _IsWindowsEleven ? currentDeskExplorerValue : currentDeskSessionValue;
            if (currentDeskValue != null)
            {
                currentDesktop = new Guid((byte[])currentDeskValue);
            }
            else
            {
                // The registry value is missing when the user hasn't switched the desktop at least one time before reading the registry. In this case we can set it to desktop one.
                // We can only set it to desktop one, if we have at least one desktop in the desktops list. Otherwise we keep the existing value.
                Log.Debug("VirtualDesktopHelper.UpdateDesktopList() failed to read the id for the current desktop form registry.", typeof(VirtualDesktopHelper));
                currentDesktop = availableDesktops.Count >= 1 ? availableDesktops[0] : currentDesktop;
            }
        }

        /// <summary>
        /// Returns an ordered list with the ids of all existing desktops. The list is ordered in the same way as the existing desktops.
        /// </summary>
        /// <returns>List of desktop ids or an empty list on failure.</returns>
        public List<Guid> GetDesktopIdList()
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            return availableDesktops;
        }

        /// <summary>
        /// Returns an ordered list with of all existing desktops and their properties. The list is ordered in the same way as the existing desktops.
        /// </summary>
        /// <returns>List of desktops or an empty list on failure.</returns>
        public List<VDesktop> GetDesktopList()
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            var list = new List<VDesktop>();
            foreach (var d in availableDesktops)
            {
                list.Add(CreateVDesktopInstance(d));
            }

            return list;
        }

        /// <summary>
        /// Returns the count of existing desktops
        /// </summary>
        /// <returns>Number of existing desktops or zero on failure.</returns>
        public int GetDesktopCount()
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            return availableDesktops.Count;
        }

        /// <summary>
        /// Returns the id of the desktop that is currently visible to the user.
        /// </summary>
        /// <returns>The <see cref="Guid"/> of the current desktop. Or <see cref="Guid.Empty"/> on failure and if we don't know the current desktop.</returns>
        public Guid GetCurrentDesktopId()
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            return currentDesktop;
        }

        /// <summary>
        /// Returns an instance of <see cref="VDesktop"/> for the desktop that is currently visible to the user.
        /// </summary>
        /// <returns>An instance of <see cref="VDesktop"/> for the current desktop, or an empty instance of <see cref="VDesktop"/> on failure.</returns>
        public VDesktop GetCurrentDesktop()
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            return CreateVDesktopInstance(currentDesktop);
        }

        /// <summary>
        /// Checks if a desktop is currently visible to the user.
        /// </summary>
        /// <param name="desktop">The guid of the desktop to check.</param>
        /// <returns><see langword="True"/> if the guid belongs to the currently visible desktop. <see langword="False"/> if not or if we don't know the visible desktop.</returns>
        public bool IsDesktopVisible(Guid desktop)
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            return currentDesktop == desktop;
        }

        /// <summary>
        /// Returns the number (position) of a desktop.
        /// </summary>
        /// <param name="desktop">The guid of the desktop.</param>
        /// <returns>Number of the desktop, if found. Otherwise a value of zero.</returns>
        public int GetDesktopNumber(Guid desktop)
        {
            if (_desktopListAutoUpdate)
            {
                UpdateDesktopList();
            }

            // Adding +1 because index starts with zero and humans start counting with one.
            return availableDesktops.IndexOf(desktop) + 1;
        }

        /// <summary>
        /// Returns the name of a desktop
        /// </summary>
        /// <param name="desktop">Guid of the desktop</param>
        /// <returns>Returns the name of the desktop or <see cref="string.Empty"/> on failure.</returns>
        public string GetDesktopName(Guid desktop)
        {
            if (desktop == Guid.Empty || !GetDesktopIdList().Contains(desktop))
            {
                Log.Debug($"VirtualDesktopHelper.GetDesktopName() failed. Parameter contains an invalid desktop guid ({desktop}) that doesn't belongs to an available desktop. Maybe the guid belongs to the generic 'AllDesktops' view.", typeof(VirtualDesktopHelper));
                return string.Empty;
            }

            // If the desktop name was not changed by the user, it isn't saved to the registry. Then we need the default name for the desktop.
            var defaultName = string.Format(System.Globalization.CultureInfo.InvariantCulture, Resources.VirtualDesktopHelper_Desktop, GetDesktopNumber(desktop));

            var registryPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VirtualDesktops\\Desktops\\{" + desktop.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) + "}";
            var deskSubKey = Registry.CurrentUser.OpenSubKey(registryPath, false);
            var desktopName = deskSubKey?.GetValue("Name");

            return desktopName != null ? (string)desktopName : defaultName;
        }

        /// <summary>
        /// Returns the position type for a desktop.
        /// </summary>
        /// <param name="desktop">Guid of the desktop.</param>
        /// <returns>Type of <see cref="VirtualDesktopPosition"/>. On failure we return <see cref="VirtualDesktopPosition.Unknown"/>.</returns>
        public VirtualDesktopPosition GetDesktopPositionType(Guid desktop)
        {
            var desktopNumber = GetDesktopNumber(desktop);
            var desktopCount = GetDesktopCount();

            if (desktopCount == 0 || desktop == Guid.Empty)
            {
                // On failure or empty guid
                return VirtualDesktopPosition.NotApplicable;
            }
            else if (desktopNumber == 1)
            {
                return VirtualDesktopPosition.FirstDesktop;
            }
            else if (desktopNumber == desktopCount)
            {
                return VirtualDesktopPosition.LastDesktop;
            }
            else if (desktopNumber > 1 & desktopNumber < desktopCount)
            {
                return VirtualDesktopPosition.BetweenOtherDesktops;
            }
            else
            {
                // All desktops view or a guid that doesn't belong to an existing desktop
                return VirtualDesktopPosition.NotApplicable;
            }
        }

        /// <summary>
        /// Returns the desktop id for a window.
        /// </summary>
        /// <param name="hWindow">Handle of the window.</param>
        /// <param name="desktopId">The guid of the desktop, where the window is shown.</param>
        /// <returns>HResult of the called method as integer.</returns>
        public int GetWindowDesktopId(IntPtr hWindow, out Guid desktopId)
        {
            if (_virtualDesktopManager == null)
            {
                Log.Error("VirtualDesktopHelper.GetWindowDesktopId() failed: The instance of <IVirtualDesktopHelper> isn't available.", typeof(VirtualDesktopHelper));
                desktopId = Guid.Empty;
                return unchecked((int)HRESULT.E_UNEXPECTED);
            }

            return _virtualDesktopManager.GetWindowDesktopId(new Windows.Win32.Foundation.HWND((nint)hWindow), out desktopId);
        }

        /// <summary>
        /// Returns an instance of <see cref="VDesktop"/> for the desktop where the window is assigned to.
        /// </summary>
        /// <param name="hWindow">Handle of the window.</param>
        /// <returns>An instance of <see cref="VDesktop"/> for the desktop where the window is assigned to, or an empty instance of <see cref="VDesktop"/> on failure.</returns>
        public VDesktop GetWindowDesktop(IntPtr hWindow)
        {
            if (_virtualDesktopManager == null)
            {
                Log.Error("VirtualDesktopHelper.GetWindowDesktop() failed: The instance of <IVirtualDesktopHelper> isn't available.", typeof(VirtualDesktopHelper));
                return CreateVDesktopInstance(Guid.Empty);
            }
            int hr = _virtualDesktopManager.GetWindowDesktopId((Windows.Win32.Foundation.HWND)hWindow, out Guid desktopId);
            return hr != (int)HRESULT.S_OK || desktopId == Guid.Empty ? VDesktop.Empty : CreateVDesktopInstance(desktopId, hWindow);
        }

        /// <summary>
        /// Returns the desktop assignment type for a window.
        /// </summary>
        /// <param name="hWindow">Handle of the window.</param>
        /// <param name="desktop">Optional the desktop id if known</param>
        /// <returns>Type of <see cref="VirtualDesktopAssignmentType"/>.</returns>
        public VirtualDesktopAssignmentType GetWindowDesktopAssignmentType(IntPtr hWindow, Guid? desktop = null)
        {
            if (_virtualDesktopManager == null)
            {
                Log.Error("VirtualDesktopHelper.GetWindowDesktopAssignmentType() failed: The instance of <IVirtualDesktopHelper> isn't available.", typeof(VirtualDesktopHelper));
                return VirtualDesktopAssignmentType.Unknown;
            }

            _ = _virtualDesktopManager.IsWindowOnCurrentVirtualDesktop((HWND)hWindow, out var isOnCurrentDesktop);
            var windowDesktopId = desktop ?? Guid.Empty; // Prepare variable in case we have no input parameter for desktop
            var hResult = desktop is null ? GetWindowDesktopId(hWindow, out windowDesktopId) : 0;

            if (hResult != (int)HRESULT.S_OK)
            {
                return VirtualDesktopAssignmentType.Unknown;
            }
            if (windowDesktopId == Guid.Empty)
            {
                return VirtualDesktopAssignmentType.NotAssigned;
            }
            if (isOnCurrentDesktop && !GetDesktopIdList().Contains(windowDesktopId))
            {
                // These windows are marked as visible on the current desktop, but the desktop id doesn't belongs to an existing desktop.
                // In this case the desktop id belongs to the generic view 'AllDesktops'.
                return VirtualDesktopAssignmentType.AllDesktops;
            }
            if (isOnCurrentDesktop)
            {
                return VirtualDesktopAssignmentType.CurrentDesktop;
            }
            return VirtualDesktopAssignmentType.OtherDesktop;
        }

        /// <summary>
        /// Returns a value indicating if the window is assigned to a currently visible desktop.
        /// </summary>
        /// <param name="hWindow">Handle to the top level window.</param>
        /// <param name="desktop">Optional the desktop id if known</param>
        /// <returns><see langword="True"/> if the desktop with the window is visible or if the window is assigned to all desktops. <see langword="False"/> if the desktop is not visible and on failure,</returns>
        public bool IsWindowOnVisibleDesktop(IntPtr hWindow, Guid? desktop = null)
        {
            return GetWindowDesktopAssignmentType(hWindow, desktop) == VirtualDesktopAssignmentType.CurrentDesktop || GetWindowDesktopAssignmentType(hWindow, desktop) == VirtualDesktopAssignmentType.AllDesktops;
        }

        /// <summary>
        /// Returns a value indicating if the window is cloaked by VirtualDesktopManager.
        /// (A cloaked window is not visible to the user. But the window is still composed by DWM.)
        /// </summary>
        /// <param name="hWindow">Handle of the window.</param>
        /// <param name="desktop">Optional the desktop id if known</param>
        /// <returns>A value indicating if the window is cloaked by Virtual Desktop Manager, because it is moved to an other desktop.</returns>
        internal unsafe bool IsWindowCloakedByVirtualDesktopManager(HWND hWindow, Guid? desktop = null)
        {
            var dwmCloakedState = 0;
            // If a window is hidden because it is moved to an other desktop, then DWM returns type "CloakedShell". If DWM returns an other type the window is not cloaked by shell or VirtualDesktopManager.
            _ = PInvoke.DwmGetWindowAttribute(hWindow, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &dwmCloakedState, sizeof(uint));
            return GetWindowDesktopAssignmentType(hWindow, desktop) == VirtualDesktopAssignmentType.OtherDesktop && dwmCloakedState == (int)DwmWindowCloakStates.CloakedShell;
        }

        public static bool IsWindowPinned(IntPtr hWnd)
        {
            // return true if window is pinned to all desktops
            if (hWnd == IntPtr.Zero) throw new ArgumentNullException();
            return DesktopManager.VirtualDesktopPinnedApps.IsViewPinned(hWnd.GetApplicationView());
        }

        /// <summary>
        /// Moves the window to a specific desktop.
        /// </summary>
        /// <param name="hWindow">Handle of the top level window.</param>
        /// <param name="desktopId">Guid of the target desktop.</param>
        /// <returns><see langword="True"/> on success and <see langword="false"/> on failure.</returns>
        internal static unsafe bool MoveWindowToDesktop(HWND hWnd, IVirtualDesktop comVirtualDesktop)
        {
            // move window to this desktop
            if (hWnd == nint.Zero) throw new ArgumentNullException();
            uint processId = 0;
            _ = PInvoke.GetWindowThreadProcessId(hWnd, &processId);

            if (Process.GetCurrentProcess().Id == processId)
            {
                // window of process
                try // the easy way (if we are owner)
                {
                    DesktopManager.VirtualDesktopManager.MoveWindowToDesktop(hWnd, comVirtualDesktop.GetId());
                }
                catch // window of process, but we are not the owner
                {
                    DesktopManager.ApplicationViewCollection.GetViewForHWnd(hWnd, out var view);
                    DesktopManager.VirtualDesktopManagerInternal.MoveViewToDesktop(view, comVirtualDesktop);
                }
            }
            else
            {
                // window of other process
                DesktopManager.ApplicationViewCollection.GetViewForHWnd(hWnd, out var view);
                try
                {
                    DesktopManager.VirtualDesktopManagerInternal.MoveViewToDesktop(view, comVirtualDesktop);
                }
                catch
                {
                    // could not move active window, try main window (or whatever windows thinks is the main window)
                    DesktopManager.ApplicationViewCollection.GetViewForHWnd(
                        Process.GetProcessById((int)processId).MainWindowHandle,
                        out view);
                    DesktopManager.VirtualDesktopManagerInternal.MoveViewToDesktop(view, comVirtualDesktop);
                }
            }

            return true;
        }

        /// <summary>
        /// Move a window one desktop left.
        /// </summary>
        /// <param name="hWindow">Handle of the top level window.</param>
        /// <returns><see langword="True"/> on success and <see langword="false"/> on failure.</returns>
        internal bool MoveWindowOneDesktopLeft(HWND hWindow)
        {
            var hr = GetWindowDesktopId(hWindow, out var windowDesktop);
            if (hr != (int)HRESULT.S_OK)
            {
                Log.Error($"VirtualDesktopHelper.MoveWindowOneDesktopLeft() failed when moving the window ({hWindow}) one desktop left: Can't get current desktop of the window.", typeof(VirtualDesktopHelper));
                return false;
            }

            if (GetDesktopIdList().Count == 0 || GetWindowDesktopAssignmentType(hWindow, windowDesktop) == VirtualDesktopAssignmentType.Unknown || GetWindowDesktopAssignmentType(hWindow, windowDesktop) == VirtualDesktopAssignmentType.NotAssigned)
            {
                Log.Error($"VirtualDesktopHelper.MoveWindowOneDesktopLeft() failed when moving the window ({hWindow}) one desktop left: We can't find the target desktop. This can happen if the desktop list is empty or if the window isn't assigned to a specific desktop.", typeof(VirtualDesktopHelper));
                return false;
            }

            var windowDesktopNumber = GetDesktopIdList().IndexOf(windowDesktop);
            if (windowDesktopNumber == 1)
            {
                Log.Error($"VirtualDesktopHelper.MoveWindowOneDesktopLeft() failed when moving the window ({hWindow}) one desktop left: The window is on the first desktop.", typeof(VirtualDesktopHelper));
                return false;
            }

            var newDesktop = availableDesktops[windowDesktopNumber - 1];

            return MoveWindowToDesktop(hWindow, DesktopManager.VirtualDesktopManagerInternal.FindDesktop(ref newDesktop));
        }

        /// <summary>
        /// Move a window one desktop right.
        /// </summary>
        /// <param name="hWindow">Handle of the top level window.</param>
        /// <returns><see langword="True"/> on success and <see langword="false"/> on failure.</returns>
        internal bool MoveWindowOneDesktopRight(HWND hWindow)
        {
            var hr = GetWindowDesktopId(hWindow, out var windowDesktop);
            if (hr != (int)HRESULT.S_OK)
            {
                Log.Error($"VirtualDesktopHelper.MoveWindowOneDesktopRight() failed when moving the window ({hWindow}) one desktop right: Can't get current desktop of the window.", typeof(VirtualDesktopHelper));
                return false;
            }

            if (GetDesktopIdList().Count == 0 || GetWindowDesktopAssignmentType(hWindow, windowDesktop) == VirtualDesktopAssignmentType.Unknown || GetWindowDesktopAssignmentType(hWindow, windowDesktop) == VirtualDesktopAssignmentType.NotAssigned)
            {
                Log.Error($"VirtualDesktopHelper.MoveWindowOneDesktopRight() failed when moving the window ({hWindow}) one desktop right: We can't find the target desktop. This can happen if the desktop list is empty or if the window isn't assigned to a specific desktop.", typeof(VirtualDesktopHelper));
                return false;
            }

            var windowDesktopNumber = GetDesktopIdList().IndexOf(windowDesktop);
            if (windowDesktopNumber == GetDesktopCount())
            {
                Log.Error($"VirtualDesktopHelper.MoveWindowOneDesktopRight() failed when moving the window ({hWindow}) one desktop right: The window is on the last desktop.", typeof(VirtualDesktopHelper));
                return false;
            }

            var newDesktop = availableDesktops[windowDesktopNumber + 1];
            return MoveWindowToDesktop(hWindow, DesktopManager.VirtualDesktopManagerInternal.FindDesktop(ref newDesktop));
        }

        /// <summary>
        /// Returns an instance of <see cref="VDesktop"/> for a Guid.
        /// </summary>
        /// <param name="desktop">Guid of the desktop.</param>
        /// <param name="hWindow">Handle of the window shown on the desktop. If this parameter is set we can detect if it is the AllDesktops view.</param>
        /// <returns>A <see cref="VDesktop"/> instance. If the parameter desktop is <see cref="Guid.Empty"/>, we return an empty <see cref="VDesktop"/> instance.</returns>
        private VDesktop CreateVDesktopInstance(Guid desktop, IntPtr hWindow = default)
        {
            if (desktop == Guid.Empty)
            {
                return VDesktop.Empty;
            }

            // Can be only detected if method is invoked with window handle parameter.
            var desktopType = hWindow != default ? GetWindowDesktopAssignmentType(hWindow, desktop) : VirtualDesktopAssignmentType.Unknown;
            var isAllDesktops = hWindow != default && desktopType == VirtualDesktopAssignmentType.AllDesktops;
            var isDesktopVisible = hWindow != default ? isAllDesktops || desktopType == VirtualDesktopAssignmentType.CurrentDesktop : IsDesktopVisible(desktop);
            var comVirtualDesktop = DesktopManager.VirtualDesktopManagerInternal.FindDesktop(ref desktop);
            return new VDesktop()
            {
                Id = desktop,
                Name = isAllDesktops ? Resources.VirtualDesktopHelper_AllDesktops : GetDesktopName(desktop),
                Number = GetDesktopNumber(desktop),
                IsVisible = isDesktopVisible || isAllDesktops,
                IsAllDesktopsView = isAllDesktops,
                Position = GetDesktopPositionType(desktop),
                ComVirtualDesktop = comVirtualDesktop
            };
        }

        /// <summary>
        /// Check if we running on Windows 11 or later.
        /// </summary>
        /// <returns><see langword="True"/> if yes and <see langword="false"/> if no.</returns>
        private static bool IsWindowsElevenOrLater()
        {
            var currentBuildString = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", false)?.GetValue("CurrentBuild", null) ?? uint.MinValue;
            var currentBuild = uint.TryParse(currentBuildString as string, out var build) ? build : uint.MinValue;

            var currentBuildNumberString = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", false)?.GetValue("CurrentBuildNumber", null) ?? uint.MinValue;
            var currentBuildNumber = uint.TryParse(currentBuildNumberString as string, out var buildNumber) ? buildNumber : uint.MinValue;

            var currentWindowsBuild = currentBuild != uint.MinValue ? currentBuild : currentBuildNumber;
            return currentWindowsBuild >= 22000;
        }
    }

    /// <summary>
    /// Enum to show in which way a window is assigned to a desktop
    /// </summary>
    public enum VirtualDesktopAssignmentType
    {
        Unknown = -1,
        NotAssigned = 0,
        AllDesktops = 1,
        CurrentDesktop = 2,
        OtherDesktop = 3,
    }

    /// <summary>
    /// Enum to show the position of a desktop in the list of all desktops
    /// </summary>
    public enum VirtualDesktopPosition
    {
        FirstDesktop,
        BetweenOtherDesktops,
        LastDesktop,
        NotApplicable, // If not applicable or unknown
    }
}
