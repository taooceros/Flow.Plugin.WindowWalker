// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Flow.Launcher.Plugin;
using Flow.Plugin.WindowWalker.Components;
using Flow.Plugin.WindowWalker.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using static Flow.Plugin.WindowWalker.Properties.Resources;
using ContextMenu = Flow.Plugin.WindowWalker.Components.ContextMenu;

namespace Flow.Plugin.WindowWalker
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IDisposable
    {
        internal static readonly Dictionary<string, Window> cachedWindows = new Dictionary<string, Window>();

        private static IEnumerable<SearchResult> searchResults;

        public const string IconPath = "Images/windowwalker.light.png";

        // High score to displayed on top against results 
        // that are frequently selected by user which recieve a boost too from Flow.
        public const int cachedWindowsScore = 500;

        private SettingWindow SettingWindow;
        private bool disposedValue;

        public static PluginInitContext Context { get; private set; }
        public static Settings Settings { get; private set; }

        internal static readonly VirtualDesktopHelper VirtualDesktopHelperInstance = new VirtualDesktopHelper();

        public List<Result> Query(Query query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (cachedWindows.ContainsKey(query.Search))
            {
                if (cachedWindows[query.Search].IsWindow)
                {
                    var window = cachedWindows[query.Search];
                    return new List<Result>
                    {
                        new Result
                        {
                            Title = window.Title,
                            IcoPath = IconPath,
                            Score = cachedWindowsScore,
                            SubTitle = $"{wox_plugin_windowwalker_running} : {window.Process.Name}",
                            ContextData = window,
                            Action = c =>
                            {
                                window.SwitchToWindow();
                                return true;
                            }
                        }
                    };
                }
                else
                {
                    cachedWindows.Remove(query.Search);
                }
            }

            VirtualDesktopHelperInstance.UpdateDesktopList();
            OpenWindows.Instance.UpdateOpenWindowsList();

            searchResults = SearchController.GetResult(query.Search);

            var results = searchResults.Where(x => !string.IsNullOrEmpty(x.Result.Title))
                .Select(x => new Result()
                {
                    Title = x.Result.Title,
                    IcoPath = IconPath,
                    Score = x.Score,
                    TitleHighlightData = x.SearchMatchesInTitle?.MatchData,
                    SubTitle =
                        $"{wox_plugin_windowwalker_running} : {x.Result.Process.Name}{(VirtualDesktopHelperInstance.GetDesktopCount() > 1 ? $" - {wox_plugin_windowwalker_Desktop}: {x.Result.Desktop.Name}" : "")}",
                    ContextData = x.Result,
                    Action = c =>
                    {
                        if (c.SpecialKeyState.CtrlPressed)
                        {
                            x.Result.CloseThisWindow(true);
                        }
                        else
                        {
                            x.Result.SwitchToWindow();
                        }

                        return true;
                    },
                }).ToList();

            foreach (var result in results)
            {
                foreach (var cache in cachedWindows.Where(cache => cache.Value.Title == result.Title))
                {
                    result.Title = $"{cache.Key} - {result.Title}";

                    if (string.IsNullOrEmpty(query.Search))
                        result.Score = cachedWindowsScore;
                }
            }
            return results.OrderBy(x => x.Title).ToList();
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
            Settings = Context.API.LoadSettingJsonStorage<Settings>();
            Log.API = Context.API;
            RegisterQuickAccessKeyword();
            OpenWindows.Instance.UpdateOpenWindowsList();
        }

        public void RegisterQuickAccessKeyword()
        {
            Context.API.RegisterGlobalKeyboardCallback(API_GlobalKeyboardEvent);
        }

        private bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
            if (Settings.EnableQuickAccessHotKey &&
                keyevent == 256 && vkcode == (int)Settings.QuickAccessHotKey.Key &&
                state.CtrlPressed == Settings.QuickAccessHotKey.Ctrl &&
                state.AltPressed == Settings.QuickAccessHotKey.Alt &&
                state.ShiftPressed == Settings.QuickAccessHotKey.Shift &&
                state.WinPressed == Settings.QuickAccessHotKey.Win) // 68 is D
            {
                var foreGroundWindowPtr = NativeMethods.GetForegroundWindow();
                Window foreGroundWindow = new Window(foreGroundWindowPtr);

                var quickAccessWindow = new QuickAccessKeywordAssignedWindow(foreGroundWindow);
                quickAccessWindow.ShowDialog();

                return false;
            }
            return true;
        }

        public string GetTranslatedPluginTitle()
        {

            return wox_plugin_windowwalker_plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return wox_plugin_windowwalker_plugin_description;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult == null)
                return new List<Result>();
            return selectedResult.ContextData is not Window window ? new List<Result>() : ContextMenu.GetContextMenu(window);

        }

        public Control CreateSettingPanel()
        {
            return SettingWindow ??= new SettingWindow(Settings);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SettingWindow?.Dispose();
                }

                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Main()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}