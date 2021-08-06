// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Plugin.WindowWalker.Components;
using Flow.Launcher.Plugin;
using Microsoft.Plugin.WindowWalker.Views;
using System.Windows.Controls;

namespace Microsoft.Plugin.WindowWalker
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IDisposable
    {
        internal readonly static Dictionary<string, Window> cachedWindows = new Dictionary<string, Window>();

        private static IEnumerable<SearchResult> searchResults;

        public const string IconPath = "Images/windowwalker.light.png";

        // High score to displayed on top against results 
        // that are frequently selected by user which recieve a boost too from Flow.
        public const int cachedWindowsScore = 500;

        private SettingWindow SettingWindow;
        private bool disposedValue;

        public static PluginInitContext Context { get; private set; }
        public Settings Settings { get; private set; }

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
                            IcoPath= IconPath,
                            Score= cachedWindowsScore,
                            SubTitle = $"{Properties.Resources.wox_plugin_windowwalker_running} : {window.ProcessName}",
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

            OpenWindows.Instance.UpdateOpenWindowsList();

            searchResults = SearchController.GetResult(query.Search);

            var results = searchResults.Where(x => !string.IsNullOrEmpty(x.Result.Title))
                          .Select(x => new Result()
                          {
                              Title = x.Result.Title,
                              IcoPath = IconPath,
                              Score = x.Score,
                              TitleHighlightData = x.SearchMatchesInTitle?.MatchData,
                              SubTitle = $"{Properties.Resources.wox_plugin_windowwalker_running} : {x.Result.ProcessName}",
                              ContextData = x.Result,
                              Action = c =>
                              {
                                  if (c.SpecialKeyState.CtrlPressed)
                                  {
                                      x.Result.Close();
                                  }
                                  else
                                  {
                                      x.Result.SwitchToWindow();
                                  }

                                  return true;
                              },
                          }).ToList();

            for (int i = 0; i < results.Count; i++)
            {
                foreach (var cache in cachedWindows)
                {
                    if (cache.Value.Title == results[i].Title)
                    {
                        results[i].Title = $"{cache.Key} - {results[i].Title}";

                        if (string.IsNullOrEmpty(query.Search))
                            results[i].Score = cachedWindowsScore;
                    }
                }
            }

            return results.OrderBy(x => x.Title).ToList();
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
            Settings = Context.API.LoadSettingJsonStorage<Settings>();
            RegisterQuickAccessKeyword();
            OpenWindows.Instance.UpdateOpenWindowsList();
            SettingWindow = new SettingWindow(Settings);
        }

        public void RegisterQuickAccessKeyword()
        {
            Context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        private bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
            if (keyevent == 256 && vkcode == (int)Settings.QuickAccessKeyword.Key &&
                state.CtrlPressed == Settings.QuickAccessKeyword.Ctrl &&
                state.AltPressed == Settings.QuickAccessKeyword.Alt &&
                state.ShiftPressed == Settings.QuickAccessKeyword.Shift &&
                state.WinPressed == Settings.QuickAccessKeyword.Win) // 68 is D
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
            return Properties.Resources.wox_plugin_windowwalker_plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.wox_plugin_windowwalker_plugin_description;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult == null)
                return new List<Result>();

            var window = selectedResult.ContextData as Window;

            return window.ContextMenu();
        }

        public Control CreateSettingPanel()
        {
            return new SettingWindow(this.Settings);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                SettingWindow.Dispose();

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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