// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Plugin.WindowWalker.Components;
using Flow.Launcher.Plugin;

namespace Microsoft.Plugin.WindowWalker
{
    public class Main : IPlugin, IPluginI18n
    {
        private static List<SearchResult> _results = new List<SearchResult>();

        private string IconPath { get; set; } = "Images/windowwalker.light.png";

        private PluginInitContext Context { get; set; }

        static Main()
        {
            SearchController.Instance.OnSearchResultUpdateEventHandler += SearchResultUpdated;
            OpenWindows.Instance.UpdateOpenWindowsList();
        }

        public List<Result> Query(Query query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            
            OpenWindows.Instance.UpdateOpenWindowsList();
            SearchController.Instance.UpdateSearchText(query.Search).Wait();

            return _results.Select(x => new Result()
            {
                Title = x.Result.Title,
                IcoPath = IconPath,
                Score = x.Score,
                SubTitle = Properties.Resources.wox_plugin_windowwalker_running + ": " + x.Result.ProcessName,
                Action = c =>
                {
                    x.Result.SwitchToWindow();
                    return true;
                },
            }).ToList();
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
        }



        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.wox_plugin_windowwalker_plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.wox_plugin_windowwalker_plugin_description;
        }

        private static void SearchResultUpdated(object sender, SearchController.SearchResultUpdateEventArgs e)
        {
            _results = SearchController.Instance.SearchMatches;
        }
    }
}
