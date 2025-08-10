using System;
using Flow.Launcher.Plugin;
using Flow.Plugin.WindowWalker.Views;
using System.Collections.Generic;
using Flow.Plugin.WindowWalker.Properties;
using BrowserTabs;

namespace Flow.Plugin.WindowWalker.Components
{
    public static class ContextMenu
    {
        public static List<Result> GetContextMenu(Window window)
        {
            var contextMenuItems = new List<Result>
            {
                new Result
                {
                    Title = Resources.ContextMenu_QuickAccess,
                    IcoPath = Main.IconPath,
                    Action = _ =>
                    {
                        var quickAccessAssign = new QuickAccessKeywordAssignedWindow(window);
                        quickAccessAssign.ShowDialog();
                        return true;
                    }
                }
            };

            var currentDesktop = Main.VirtualDesktopHelperInstance.GetCurrentDesktop();
            var isWindowPinned = VirtualDesktopHelper.IsWindowPinned(window.Hwnd);
            if (isWindowPinned.HasValue && !isWindowPinned.Value && window.Desktop.Id != currentDesktop.Id &&
                currentDesktop.ComVirtualDesktop is not null)
            {
                contextMenuItems.Add(new Result
                {
                    Title = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        Resources.ContextMenu_MoveToCurrentDesktop, currentDesktop.Name),
                    IcoPath = Main.IconPath,
                    Action = _ =>
                    {
                        VirtualDesktopHelper.MoveWindowToDesktop(window.Hwnd, currentDesktop.ComVirtualDesktop);
                        window.SwitchToWindow();
                        return true;
                    }
                });
            }

            return contextMenuItems;
        }

        /// <summary>
        /// Gets context menu for browser tabs
        /// </summary>
        /// <param name="tab">The browser tab</param>
        /// <returns>List of context menu items</returns>
        internal static List<Result> GetBrowserTabContextMenu(BrowserTab tab)
        {
            var results = new List<Result>
            {
                new Result
                {
                    Title = Resources.BrowserTab_SwitchToTab,
                    IcoPath = Main.IconPath,
                    Action = c =>
                    {
                        BrowserTabManager.ActivateTab(tab);
                        return true;
                    }
                },
                new Result
                {
                    Title = Resources.BrowserTab_CloseTab,
                    IcoPath = Main.IconPath,
                    Action = c =>
                    {
                        BrowserTabManager.CloseTab(tab);
                        return true;
                    }
                }
            };

            return results;
        }
    }
}