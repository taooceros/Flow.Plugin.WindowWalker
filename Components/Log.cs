// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Flow.Launcher.Plugin;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Flow.Plugin.WindowWalker.Components
{
    internal static class Log
    {
        internal static IPublicAPI API { get; set; }

        internal static void Exception(string v, Exception ex, Type type, [CallerMemberName] string methodName = "")
        {
            API.LogException(type.FullName, v, ex, methodName);
        }

        internal static void Debug(string v, Type type, [CallerMemberName] string methodName = "")
        {
            API.LogDebug(type.FullName, v, methodName);
        }

        internal static void Error(string v, Type type, [CallerMemberName] string methodName = "")
        {
            API.LogException(type.FullName, v, new Exception(v) , methodName);
        }
    }
}