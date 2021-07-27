using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.PackageManager;

namespace ConsoleTiny
{
    /// <summary>
    /// 用于打开目标文件
    /// </summary>
    public static class ScriptAssetOpener
    {
        /// <summary>
        /// 打开Asset中的文件
        /// </summary>
        /// <param name="file">文件名</param>
        /// <param name="line">目标行号</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenAsset(string file, int line)
        {
            if (string.IsNullOrEmpty(file) || file == "None")
            {
                return false;
            }
            if (file.StartsWith("Assets/"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file);
                if (obj)
                {
                    AssetDatabase.OpenAsset(obj, line);
                    return true;
                }

                return false;
            }

            char separatorChar = '\\';
            string fileFullPath;
            fileFullPath = Path.GetFullPath(file.Replace('/', separatorChar));

            var packageInfos = Packages.GetAll();
            foreach (var packageInfo in packageInfos)
            {
                if (fileFullPath.StartsWith(packageInfo.resolvedPath, StringComparison.Ordinal))
                {
                    InternalEditorUtility.OpenFileAtLineExternal(fileFullPath, line);
                    return true;
                }
            }

            // 别人编译的DLL，不存在文件路径，那么就以工程路径拼接组装来尝试获取本地路径
            if (!File.Exists(fileFullPath))
            {
                string directoryName = Directory.GetCurrentDirectory();
                while (true)
                {
                    if (string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName))
                    {
                        return false;
                    }

                    int pos = fileFullPath.IndexOf(separatorChar);
                    while (pos != -1)
                    {
                        string testFullPath = Path.Combine(directoryName, fileFullPath.Substring(pos + 1));
                        if (File.Exists(testFullPath) && TryOpenVisualStudioFile(testFullPath, line))
                        {
                            return true;
                        }

                        pos = fileFullPath.IndexOf(separatorChar, pos + 1);
                    }

                    directoryName = Path.GetDirectoryName(directoryName);
                }
            }

            return TryOpenVisualStudioFile(fileFullPath, line);
        }

        /// <summary>
        /// 尝试打开在VisualStudio文件
        /// </summary>
        /// <param name="file">文件名</param>
        /// <param name="line">目标行号</param>
        /// <returns>是否成功打开</returns>
        private static bool TryOpenVisualStudioFile(string file, int line)
        {
            string dirPath = file;

            do
            {
                dirPath = Path.GetDirectoryName(dirPath);
                if (!string.IsNullOrEmpty(dirPath) && Directory.Exists(dirPath))
                {
                    var files = Directory.GetFiles(dirPath, "*.sln", SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        OpenVisualStudioFile(files[0], file, line);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            } while (true);

            return false;
        }

        /// <summary>
        /// 打VisualStudio文件
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="file">文件名</param>
        /// <param name="line">行号</param>
        private static void OpenVisualStudioFile(string projectPath, string file, int line)
        {
            string vsPath = ScriptEditorUtility.GetExternalScriptEditor();
            if (string.IsNullOrEmpty(vsPath) || !File.Exists(vsPath))
            {
                return;
            }
            string exePath = String.Empty;

            var packageInfos = Packages.GetAll();
            foreach (var packageInfo in packageInfos)
            {
                if (packageInfo.name == "com.wuhuan.consoletiny")
                {
                    exePath = packageInfo.resolvedPath;
                    // https://github.com/akof1314/VisualStudioFileOpenTool
                    exePath = exePath + "\\Editor\\VisualStudioFileOpenTool.exe";
                    break;
                }
            }

            if (string.IsNullOrEmpty(exePath))
            {
                exePath = "Assets/Editor/VisualStudioFileOpenTool.exe";
            }

            if (!string.IsNullOrEmpty(exePath))
            {
                if (!File.Exists(exePath))
                {
                    return;
                }

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    OpenVisualStudioFileInter(exePath, vsPath, projectPath, file, line);
                });
            }
        }


        private static void OpenVisualStudioFileInter(string exePath, string vsPath, string projectPath, string file, int line)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Format("{0} {1} {2} {3}",
                   vsPath.QuotePathIfNeeded(), projectPath.QuotePathIfNeeded(), file.QuotePathIfNeeded(), line),
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }
}