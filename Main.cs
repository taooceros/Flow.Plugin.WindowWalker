// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Plugin.WindowWalker.Components;
using Flow.Launcher.Plugin;
using System.Xml.Linq;
using System.Windows.Input;
using Microsoft.Plugin.WindowWalker.Views;

namespace Microsoft.Plugin.WindowWalker
{
    public class Main : IPlugin, IPluginI18n, IContextMenu
    {
        internal readonly static Dictionary<string, Window> cachedWindows = new Dictionary<string, Window>();

        private static IEnumerable<SearchResult> _results;

        public const string IconPath = "Images/windowwalker.light.png";

        public static PluginInitContext Context { get; private set; }

        private ContextMenu contextMenu = new ContextMenu();

        static Main()
        {
            OpenWindows.Instance.UpdateOpenWindowsList();
        }

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
                            Score=100,
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
            _results = SearchController.Instance.UpdateSearchText(query.Search);

            return _results.Select(x => new Result()
            {
                Title = x.Result.Title,
                IcoPath = IconPath,
                Score = x.Score,
                SubTitle = $"{Properties.Resources.wox_plugin_windowwalker_running} : {x.Result.ProcessName}",
                ContextData = x.Result,
                Action = c =>
                {
                    if (c.SpecialKeyState.CtrlPressed)
                        x.Result.Close();
                    else 
                        x.Result.SwitchToWindow();
                    return true;
                },
            }).ToList();
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
            RegisterQuickAccessKeyword();
        }

        public void RegisterQuickAccessKeyword()
        {
            Context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        private bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
            if (keyevent == 256 && vkcode == 68 && state.CtrlPressed && state.AltPressed) // 68 is D
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
            return contextMenu.LoadContextMenus(selectedResult);
        }
    }
}
