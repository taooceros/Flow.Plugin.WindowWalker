// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code forked from Betsegaw Tadele's https://github.com/betsegaw/windowwalker/
using Flow.Launcher.Plugin.SharedModels;
using System.Collections.Generic;

namespace Microsoft.Plugin.WindowWalker.Components
{
    /// <summary>
    /// Contains search result windows with each window including the reason why the result was included
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Gets the actual window reference for the search result
        /// </summary>
        public Window Result
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the list of indexes of the matching characters for the search in the title window
        /// </summary>
        public MatchResult SearchMatchesInTitle
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of indexes of the matching characters for the search in the
        /// name of the process
        /// </summary>
        public MatchResult SearchMatchesInProcessName
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets a score indicating how well this matches what we are looking for
        /// </summary>
        public int Score
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the source of where the best score was found
        /// </summary>
        public TextType BestScoreSource
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// Constructor
        /// </summary>
        public SearchResult(Window window, MatchResult titleMatch, MatchResult processMatch)
        {
            Result = window;
            SearchMatchesInTitle = titleMatch;
            SearchMatchesInProcessName = processMatch;
            GetBestScore();
        }

        public SearchResult() { }

        /// <summary>
        /// Calculates the score for how closely this window matches the search string
        /// </summary>
        /// <remarks>
        /// Higher Score is better
        /// </remarks>
        private void GetBestScore()
        {
            if (SearchMatchesInTitle.Score > SearchMatchesInProcessName.Score)
            {
                Score = SearchMatchesInTitle.Score;
                BestScoreSource = TextType.WindowTitle;
            }
            else
            {
                Score = SearchMatchesInProcessName.Score;
                BestScoreSource = TextType.ProcessName;
            }
        }

        /// <summary>
        /// The type of text that a string represents
        /// </summary>
        public enum TextType
        {
            ProcessName,
            WindowTitle,
        }

        /// <summary>
        /// The type of search
        /// </summary>
        public enum SearchType
        {
            /// <summary>
            /// the search string is empty, which means all open windows are
            /// going to be returned
            /// </summary>
            Empty,

            /// <summary>
            /// Regular fuzzy match search
            /// </summary>
            Fuzzy,

            /// <summary>
            /// The user has entered text that has been matched to a shortcut
            /// and the shortcut is now being searched
            /// </summary>
            Shortcut,
        }
    }
}
