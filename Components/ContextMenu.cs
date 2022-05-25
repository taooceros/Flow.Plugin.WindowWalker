using Flow.Launcher.Plugin;
using Microsoft.Plugin.WindowWalker.Views;
using System.Collections.Generic;

namespace Microsoft.Plugin.WindowWalker.Components
{
    public static class ContextMenu
    {
        
        public static List<Result> GetContextMenu(Window window)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Create Quick Access for this window",
                    IcoPath = Main.IconPath,
                    Action = _ =>
                    {
                        var quickAccessAssign = new QuickAccessKeywordAssignedWindow(window);
                        quickAccessAssign.ShowDialog();
                        return true;
                    }
                }
            };
        }
    }
}