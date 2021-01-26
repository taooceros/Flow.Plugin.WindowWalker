using Flow.Launcher.Plugin;
using Microsoft.Plugin.WindowWalker.Components;
using Microsoft.Plugin.WindowWalker.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Plugin.WindowWalker
{
    class ContextMenu : IContextMenu
    {
        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult == null)
                return new List<Result>();

            var window = selectedResult.ContextData as Window;

            return window.ContextMenu();
        }
    }
}
