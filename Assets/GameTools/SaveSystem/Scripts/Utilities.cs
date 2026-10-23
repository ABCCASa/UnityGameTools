using System;
using System.Collections.Generic;
using System.IO;

namespace GameTools.SaveSystem 
{
    internal static class Utilities
    {
        private static readonly char[] pathSeps = { '/', '\\' };
        private static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        public static string ValidAndNormalizePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                throw new ArgumentException("path cannot be null, empty, or whitespace.");
            string[] splits = rawPath.Split(pathSeps);
            for (int i = 0; i < splits.Length; i++)
            {
                string part = splits[i].Trim();
                if (string.IsNullOrWhiteSpace(part)) throw new ArgumentException($"Empty path segment is not allowed\n original path: '{rawPath}'");
                if (part.EndsWith('.'))  //屏蔽掉 "." ".." ".. .." "xxx." 等写法
                {
                    throw new ArgumentException($"Path segment: '{part}' is not allowed\n original path: '{rawPath}'");
                }
                if (part.IndexOfAny(invalidFileNameChars) >= 0)
                {
                    throw new ArgumentException($"Path segment: '{part}' contain invalid File name char\n original path: '{rawPath}'");
                }
                splits[i] = part;
            }
            string normalizedPath = string.Join("/",  splits);
            return normalizedPath;
        }
    }
    
    
}
