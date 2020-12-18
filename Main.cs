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
        private static IEnumerable<SearchResult> _results;

        private string IconPath { get; set; } = "Images/windowwalker.light.png";

        public static PluginInitContext Context { get; private set; }

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

            OpenWindows.Instance.UpdateOpenWindowsList();
            _results = SearchController.Instance.UpdateSearchText(query.Search); 

            return _results.Select(x => new Result()
            {
                Title = x.Result.Title,
                IcoPath = IconPath,
                Score = x.Score,
                SubTitle = $"{Properties.Resources.wox_plugin_windowwalker_running} : {x.Result.ProcessName}",
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
    }
}
