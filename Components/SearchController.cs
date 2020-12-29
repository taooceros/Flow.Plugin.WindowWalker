﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code forked from Betsegaw Tadele's https://github.com/betsegaw/windowwalker/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Microsoft.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Responsible for searching and finding matches for the strings provided.
    /// Essentially the UI independent model of the application
    /// </summary>
    internal class SearchController
    {
        /// <summary>
        /// the current search text
        /// </summary>
        private string searchText;

        /// <summary>
        /// Open window search results
        /// </summary
        private List<SearchResult> searchMatches;

        /// <summary>
        /// Singleton pattern
        /// </summary>
        private static SearchController instance;

        /// <summary>
        /// Gets or sets the current search text
        /// </summary>
        public string SearchText
        {
            get
            {
                return searchText;
            }

            set
            {
                // Using CurrentCulture since this is user facing
                searchText = value.ToLower(CultureInfo.CurrentCulture).Trim();
            }
        }

        /// <summary>
        /// Gets singleton Pattern
        /// </summary>
        public static SearchController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SearchController();
                }

                return instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchController"/> class.
        /// Initializes the search controller object
        /// </summary>
        private SearchController()
        {
            searchText = string.Empty;
        }

        /// <summary>
        /// Event handler for when the search text has been updated
        /// </summary>
        public List<SearchResult> UpdateSearchText(string searchText)
        {
            SearchText = searchText;
            return OpenWindowsWithModel();
        }

        /// <summary>
        /// Syncs the open windows with the OpenWindows Model
        /// </summary>
        public List<SearchResult> OpenWindowsWithModel()
        {
            System.Diagnostics.Debug.Print("Syncing WindowSearch result with OpenWindows Model");

            List<Window> snapshotOfOpenWindows = OpenWindows.Instance.Windows;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return searchMatches = snapshotOfOpenWindows.Select(x => new SearchResult { Result = x }).ToList();
            }
            else
            {
                return searchMatches = FuzzySearchOpenWindows(snapshotOfOpenWindows);
            }
        }

        /// <summary>
        /// Search method that matches the title of windows with the user search text
        /// </summary>
        /// <param name="openWindows">what windows are open</param>
        /// <returns>Returns search results</returns>
        private List<SearchResult> FuzzySearchOpenWindows(List<Window> openWindows)
        {
            List<SearchString> searchStrings = new List<SearchString> { new SearchString(SearchText, SearchResult.SearchType.Fuzzy) };

            var result = new List<SearchResult>();

            foreach (var searchString in searchStrings)
            {
                foreach (var window in openWindows)
                {
                    var titleMatch = FuzzyMatching.FindBestFuzzyMatch(window.Title, searchString.SearchText);
                    var processMatch = FuzzyMatching.FindBestFuzzyMatch(window.ProcessName, searchString.SearchText);

                    if ((titleMatch.Any() || processMatch.Any()) && window.Title.Length != 0)
                    {
                        result.Add(new SearchResult(window, titleMatch, processMatch, searchString.SearchType));
                    }
                }
            }

            System.Diagnostics.Debug.Print("Found " + result.Count + " windows that match the search text");

            return result;
        }
    }
}
