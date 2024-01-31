using System;
using System.Diagnostics;
using Flow.Plugin.WindowWalker.Components.COM;

namespace Flow.Plugin.WindowWalker.Components
{
    public static class DesktopManager
    {
        internal static IVirtualDesktopManagerInternal? VirtualDesktopManagerInternal;
        internal static IVirtualDesktopManager? VirtualDesktopManager;
        internal static IVirtualDesktopPinnedApps? VirtualDesktopPinnedApps;
        public static IApplicationViewCollection? ApplicationViewCollection;

        static DesktopManager()
        {
            Init();
        }

        private static void Init()
        {
            var shell = (IServiceProvider10?)Activator.CreateInstance(
                Type.GetTypeFromCLSID(Guids.CLSID_ImmersiveShell)!);

            VirtualDesktopManager = (IVirtualDesktopManager?)Activator.CreateInstance(
                Type.GetTypeFromCLSID(Guids.CLSID_VirtualDesktopManager)!);

            InitializeVirtualDesktopManagerInternal(shell);

            ApplicationViewCollection = (IApplicationViewCollection?)shell?.QueryService(
                typeof(IApplicationViewCollection).GUID,
                typeof(IApplicationViewCollection).GUID);
            VirtualDesktopPinnedApps = (IVirtualDesktopPinnedApps?)shell?.QueryService(
                Guids.CLSID_VirtualDesktopPinnedApps,
                typeof(IVirtualDesktopPinnedApps).GUID);
        }

        private static void InitializeVirtualDesktopManagerInternal(IServiceProvider10? shell)
        {
            try
            {
                if (Environment.OSVersion.Version.Build >= 22631)
                    VirtualDesktopManagerInternal = (IVirtualDesktopManagerInternal?)shell?.QueryService(
                        Guids.CLSID_VirtualDesktopManagerInternal,
                        typeof(IVirtualDesktopManagerInternal).GUID);
            }
            catch (Exception e)
            {
                Main.Context.API.LogException($"Flow.Plugin.WindowWalker.{VirtualDesktopManager}",
                    "Unable to load VirtualDesktopManagerInternal", e);
                VirtualDesktopManagerInternal = null;
            }
        }

        internal static IApplicationView GetApplicationView(this IntPtr hWnd)
        {
            // get application view to window handle
            ApplicationViewCollection!.GetViewForHWnd(hWnd, out var view);
            return view;
        }
    }
}