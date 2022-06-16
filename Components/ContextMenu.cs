using Flow.Launcher.Plugin;
using Flow.Plugin.WindowWalker.Views;
using System.Collections.Generic;

namespace Flow.Plugin.WindowWalker.Components
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