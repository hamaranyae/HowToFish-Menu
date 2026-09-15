using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TrickshotMenu
{
    internal static class TrickshotLog
    {
        private const int MaxLines = 500;
        private const int MaxLineLen = 220;

        private static readonly List<string> Buffer = new List<string>(128);
        private static string _filePath;
        private static bool _fsFailed;

        public static string FilePath
        {
            get
            {
                if (_filePath == null)
                {
                    _filePath = Path.Combine(Environment.CurrentDirectory, "BepInEx", "plugins", "TrickshotMenu", "Trickshot.log");
                }
                return _filePath;
            }
        }

        public static void Init()
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(FilePath, Environment.NewLine + "==== TrickshotMenu session " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====" + Environment.NewLine);
            }
            catch
            {
                _fsFailed = true;
            }
        }

        public static void Log(string msg)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
            if (line.Length > MaxLineLen)
            {
                line = line.Substring(0, MaxLineLen);
            }
            lock (Buffer)
            {
                Buffer.Add(line);
                if (Buffer.Count > MaxLines)
                {
                    Buffer.RemoveRange(0, Buffer.Count - MaxLines);
                }
            }
            WriteFile(line);
            Debug.Log("[TrickshotMenu] " + msg);
        }

        public static void LogException(string context, Exception e)
        {
            Log(context + " -> " + e);
        }

        private static void WriteFile(string line)
        {
            if (_fsFailed)
            {
                return;
            }
            try
            {
                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
            catch
            {
                _fsFailed = true;
            }
        }

        public static void Clear()
        {
            lock (Buffer)
            {
                Buffer.Clear();
            }
            WriteFile("---- log cleared ----");
        }

        public static string[] GetLines(int max)
        {
            lock (Buffer)
            {
                int n = Mathf.Min(max, Buffer.Count);
                var r = new string[n];
                for (int i = 0; i < n; i++)
                {
                    r[i] = Buffer[Buffer.Count - n + i];
                }
                return r;
            }
        }
    }
}