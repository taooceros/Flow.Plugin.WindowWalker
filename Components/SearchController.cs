// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code forked from Betsegaw Tadele's https://github.com/betsegaw/windowwalker/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BrowserTabs;

namespace Flow.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Responsible for searching and finding matches for the strings provided.
    /// Essentially the UI independent model of the application
    /// </summary>
    internal static class SearchController
    {
        private static string searchText = string.Empty;


        /// <summary>
        /// Gets or sets the current search text
        /// </summary>
        public static string SearchText
        {
            get => searchText;

            // Using CurrentCulture since this is user facing
            set => searchText = value.ToLower(CultureInfo.CurrentCulture).Trim();
        }


        /// <summary>
        /// Event handler for when the search text has been updated
        /// </summary>
        public static List<SearchResult> GetResult(string searchText, bool searchWindowsAcrossAllVDesktop)
        {
            SearchText = searchText;
            OpenWindows.Instance.SearchWindowsAcrossAllVDesktop = searchWindowsAcrossAllVDesktop;

            var results = OpenWindowsWithModel();

            var browserTabs = GetBrowserTabResults();
            results.AddRange(browserTabs);

            return results;
        }

        /// <summary>
        /// Syncs the open windows with the OpenWindows Model
        /// </summary>
        public static List<SearchResult> OpenWindowsWithModel()
        {
            System.Diagnostics.Debug.Print("Syncing WindowSearch result with OpenWindows Model");

            var snapshotOfOpenWindows = OpenWindows.Instance.Windows;

            return string.IsNullOrWhiteSpace(SearchText)
                ? snapshotOfOpenWindows.Select(x => new SearchResult(x)).ToList()
                : FuzzySearchOpenWindows(snapshotOfOpenWindows.ToList());
        }

        /// <summary>
        /// Search method that matches the title of windows with the user search text
        /// </summary>
        /// <param name="openWindows">what windows are open</param>
        /// <returns>Returns search results</returns>
        private static List<SearchResult> FuzzySearchOpenWindows(IReadOnlyList<Window> openWindows)
        {
            var result = new List<SearchResult>(openWindows.Count);

            foreach (var window in openWindows)
            {
                var titleMatch = Main.Context.API.FuzzySearch(SearchText, window.Title);
                var processMatch = Main.Context.API.FuzzySearch(SearchText, window.Process.Name);

                if (titleMatch.IsSearchPrecisionScoreMet() || processMatch.IsSearchPrecisionScoreMet())
                    result.Add(new SearchResult(window, titleMatch, processMatch));
            }


            System.Diagnostics.Debug.Print("Found " + result.Count + " windows that match the search text");

            return result;
        }

        /// <summary>
        /// Gets browser tab search results
        /// </summary>
        /// <returns>List of search results for browser tabs</returns>
        private static List<SearchResult> GetBrowserTabResults()
        {
            var results = new List<SearchResult>();

            try
            {
                var browserTabs = BrowserTabManager.GetChromiumTabs();

                foreach (var tab in browserTabs)
                {
                    var titleMatch = Main.Context.API.FuzzySearch(SearchText, tab.Title);
                    var browserNameMatch = Main.Context.API.FuzzySearch(SearchText, tab.BrowserName);

                    if (string.IsNullOrWhiteSpace(SearchText) ||
                        titleMatch.IsSearchPrecisionScoreMet() ||
                        browserNameMatch.IsSearchPrecisionScoreMet())
                    {
                        results.Add(new SearchResult(tab, titleMatch, browserNameMatch));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception("Error getting browser tab results", ex, typeof(SearchController));
            }

            return results;
        }
    }
}
