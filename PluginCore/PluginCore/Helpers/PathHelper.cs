using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using PluginCore.Managers;

namespace PluginCore.Helpers
{
    public class PathHelper
    {
        /// <summary>
        /// Path to the current application directory
        /// </summary>
        public static string BaseDir => PluginBase.MainForm.StandaloneMode ? AppDir : UserAppDir;

        /// <summary>
        /// Path to the main application directory
        /// </summary>
        public static string AppDir => Path.GetDirectoryName(GetAssemblyPath(Assembly.GetExecutingAssembly()));

        /// <summary>
        /// Path to the user's application directory
        /// </summary>
        public static string UserAppDir
        {
            get
            {
                string userAppDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(userAppDir, DistroConfig.DISTRIBUTION_NAME);
            }
        }

        /// <summary>
        /// Path to the docs directory
        /// </summary>
        public static string DocDir => Path.Combine(AppDir, "Docs");

        /// <summary>
        /// Path to the data directory
        /// </summary>
        public static string DataDir => Path.Combine(BaseDir, "Data");

        /// <summary>
        /// Path to the snippets directory
        /// </summary>
        public static string SnippetDir
        {
            get
            {
                string custom = PluginBase.Settings.CustomSnippetDir;
                if (!string.IsNullOrEmpty(custom) && Directory.Exists(custom)) return custom;
                return Path.Combine(BaseDir, "Snippets");
            }
        }

        /// <summary>
        /// Path to the templates directory
        /// </summary>
        public static string TemplateDir
        {
            get
            {
                string custom = PluginBase.Settings.CustomTemplateDir;
                if (!string.IsNullOrEmpty(custom) && Directory.Exists(custom)) return custom;
                return Path.Combine(BaseDir, "Templates");
            }
        }

        /// <summary>
        /// Path to the project templates directory
        /// </summary>
        public static string ProjectsDir
        {
            get
            {
                string custom = PluginBase.Settings.CustomProjectsDir;
                if (!string.IsNullOrEmpty(custom) && Directory.Exists(custom)) return custom;
                return Path.Combine(AppDir, "Projects");
            }
        }

        /// <summary>
        /// Path to the settings directory
        /// </summary>
        public static string SettingDir => Path.Combine(BaseDir, "Settings");

        /// <summary>
        /// Path to the custom shortcut directory
        /// </summary>
        public static string ShortcutsDir => Path.Combine(SettingDir, "Shortcuts");

        /// <summary>
        /// Path to the themes directory
        /// </summary>
        public static string ThemesDir => Path.Combine(SettingDir, "Themes");

        /// <summary>
        /// Path to the user project templates directory
        /// </summary>
        public static string UserProjectsDir => Path.Combine(UserAppDir, "Projects");

        /// <summary>
        /// Path to the user lirbrary directory
        /// </summary>
        public static string UserLibraryDir => Path.Combine(UserAppDir, "Library");

        /// <summary>
        /// Path to the library directory
        /// </summary>
        public static string LibraryDir => Path.Combine(AppDir, "Library");

        /// <summary>
        /// Path to the plugin directory
        /// </summary>
        public static string PluginDir => Path.Combine(AppDir, "Plugins");

        /// <summary>
        /// Path to the users plugin directory
        /// </summary>
        public static string UserPluginDir => Path.Combine(UserAppDir, "Plugins");

        /// <summary>
        /// Path to the tools directory
        /// </summary>
        public static string ToolDir => Path.Combine(AppDir, "Tools");

        /// <summary>
        /// Resolve the path to the mm.cfg file
        /// </summary>
        public static string ResolveMMConfig()
        {
            string homePath = Environment.GetEnvironmentVariable("HOMEPATH");
            string homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            if (!string.IsNullOrEmpty(homeDrive) && homePath != null)
            {
                try
                {
                    string tempPath = homeDrive + homePath;
                    DirectorySecurity security = Directory.GetAccessControl(tempPath);
                    AuthorizationRuleCollection rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
                    WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                    foreach (FileSystemAccessRule rule in rules)
                    {
                        if (currentUser.User.Equals(rule.IdentityReference))
                        {
                            if (rule.AccessControlType.Equals(AccessControlType.Allow))
                            {
                                return Path.Combine(tempPath, "mm.cfg");
                            }
                        }
                    }
                }
                catch {} // Not working...
            }
            string userProfile = Environment.GetEnvironmentVariable(PlatformHelper.IsRunningOnWindows() ? "USERPROFILE" : "HOME");
            return Path.Combine(userProfile, "mm.cfg");
        }

        /// <summary>
        /// Resolve a path which may be:
        /// - absolute or
        /// - relative to base path
        /// </summary>
        public static string ResolvePath(string path) => ResolvePath(path, null);

        /// <summary>
        /// Resolve a path which may be:
        /// - absolute or
        /// - relative to a specified path, or 
        /// - relative to base path
        /// </summary>
        public static string ResolvePath(string path, string relativeTo)
        {
            if (string.IsNullOrEmpty(path)) return null;
            bool isPathNetworked = path.StartsWithOrdinal("\\\\") || path.StartsWithOrdinal("//");
            bool isPathAbsSlashed = (path.StartsWith('\\') || path.StartsWith('/')) && !isPathNetworked;
            if (isPathAbsSlashed) path = Path.GetPathRoot(AppDir) + path.Substring(1);
            if (Path.IsPathRooted(path) || isPathNetworked) return path;
            string resolvedPath;
            if (relativeTo != null)
            {
                resolvedPath = Path.Combine(relativeTo, path);
                if (Directory.Exists(resolvedPath) || File.Exists(resolvedPath)) return resolvedPath;
            }
            if (!PluginBase.MainForm.StandaloneMode)
            {
                resolvedPath = Path.Combine(UserAppDir, path);
                if (Directory.Exists(resolvedPath) || File.Exists(resolvedPath)) return resolvedPath;
            }
            resolvedPath = Path.Combine(AppDir, path);
            if (Directory.Exists(resolvedPath) || File.Exists(resolvedPath)) return resolvedPath;
            return null;
        }

        /// <summary>
        /// Converts a long path to a short representative one using ellipsis if necessary
        /// </summary>
        public static string GetCompactPath(string path)
        {
            try
            {
                if (Win32.ShouldUseWin32())
                {
                    const int max = 64;
                    StringBuilder sb = new StringBuilder(max);
                    Win32.PathCompactPathEx(sb, path, max, 0);
                    return sb.ToString();
                }

                const string pattern = @"^(w+:|)([^]+[^]+).*([^]+[^]+)$";
                const string replacement = "$1$2...$3";
                if (Regex.IsMatch(path, pattern)) return Regex.Replace(path, pattern, replacement);
                return path;
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
                return path;
            }
        }

        /// <summary>
        /// Converts a long filename to a short one
        /// </summary>
        public static string GetShortPathName(string longName)
        {
            try
            {
                if (Win32.ShouldUseWin32())
                {
                    int max = longName.Length + 1;
                    StringBuilder sb = new StringBuilder(max);
                    Win32.GetShortPathName(longName, sb, max);
                    return sb.ToString();
                }

                return longName; // For other platforms
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
                return longName;
            }
        }

        /// <summary>
        /// Converts a short filename to a long one
        /// </summary>
        public static string GetLongPathName(string shortName)
        {
            try
            {
                if (Win32.ShouldUseWin32())
                {
                    StringBuilder longNameBuffer = new StringBuilder(256);
                    Win32.GetLongPathName(shortName, longNameBuffer, longNameBuffer.Capacity);
                    return longNameBuffer.ToString();
                }

                return shortName; // For other platforms
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
                return shortName;
            }
        }

        /// <summary>
        /// Gets the correct physical path from the file system
        /// </summary>
        public static string GetPhysicalPathName(string path)
        {
            try
            {
                if (Win32.ShouldUseWin32())
                {
                    int rgflnOut = 0;
                    var r = Win32.SHILCreateFromPath(path, out var ppidl, ref rgflnOut);
                    if (r == 0)
                    {
                        StringBuilder sb = new StringBuilder(260);
                        if (Win32.SHGetPathFromIDList(ppidl, sb))
                        {
                            char sep = Path.DirectorySeparatorChar;
                            char alt = Path.AltDirectorySeparatorChar;
                            return sb.ToString().Replace(alt, sep);
                        }
                    }
                }
                return path;
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
                return path;
            }
        }

        /// <summary>
        /// Finds an app from 32-bit or 64-bit program files directories
        /// </summary>
        public static string FindFromProgramFiles(string partialPath)
        {
            // This return always x86, FlashDevelop is x86
            string programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            string toolPath = Path.Combine(programFiles, partialPath);
            if (File.Exists(toolPath)) return toolPath;
            if (programFiles.Contains(" (x86)")) // Is the app in x64 program files?
            {
                toolPath = Path.Combine(programFiles.Replace(" (x86)", ""), partialPath);
                if (File.Exists(toolPath)) return toolPath;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the 32-bit Java install path
        /// </summary>
        public static string GetJavaInstallPath()
        {
            string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
            using var rk = Registry.LocalMachine.OpenSubKey(javaKey);
            string currentVersion = rk.GetValue("CurrentVersion").ToString();
            using var key = rk.OpenSubKey(currentVersion);
            return key.GetValue("JavaHome").ToString();
        }

        private static string GetAssemblyPath(Assembly assembly)
        {
            string codeBase = assembly.CodeBase;

            if (codeBase.ToLower().StartsWith(Uri.UriSchemeFile))
            {
                // Skip over the file:// part
                int start = Uri.UriSchemeFile.Length + Uri.SchemeDelimiter.Length;

                if (codeBase[start] == '/') // third slash means a local path
                {
                    // Handle Windows Drive specifications
                    if (codeBase[start + 2] == ':')
                        ++start;
                    // else leave the last slash so path is absolute
                }
                else // It's either a Windows Drive spec or a share
                {
                    if (codeBase[start + 1] != ':')
                        start -= 2; // Back up to include two slashes
                }

                return codeBase.Substring(start);
            }

            return assembly.Location;
        }

        public class Ellipsis
        {
            /// <summary>
            /// Specifies ellipsis format and alignment.
            /// </summary>
            [Flags]
            public enum EllipsisFormat
            {
                /// <summary>
                /// Text is not modified.
                /// </summary>
                None = 0,
                /// <summary>
                /// Text is trimmed at the end of the string. An ellipsis (...) is drawn in place of remaining text.
                /// </summary>
                End = 1,
                /// <summary>
                /// Text is trimmed at the begining of the string. An ellipsis (...) is drawn in place of remaining text. 
                /// </summary>
                Start = 2,
                /// <summary>
                /// Text is trimmed in the middle of the string. An ellipsis (...) is drawn in place of remaining text.
                /// </summary>
                Middle = 3,
                /// <summary>
                /// Preserve as much as possible of the drive and filename information. Must be combined with alignment information.
                /// </summary>
                Path = 4,
                /// <summary>
                /// Text is trimmed at a word boundary. Must be combined with alignment information.
                /// </summary>
                Word = 8
            }

            /// <summary>
            /// String used as a place holder for trimmed text.
            /// </summary>
            public const string EllipsisChars = "...";

            private static readonly Regex prevWord = new Regex(@"\W*\w*$", RegexOptions.Compiled);
            private static readonly Regex nextWord = new Regex(@"\w*\W*", RegexOptions.Compiled);

            /// <summary>
            /// Truncates a text string to fit within a given width by replacing trimmed text with ellipses. 
            /// </summary>
            /// <param name="text">String to be trimmed.</param>
            /// <param name="font">Font to be used to measure the text.</param>
            /// <param name="proposedWidth">The target width to accomodate the text.</param>
            /// <param name="options">Format and alignment of ellipsis.</param>
            /// <returns>This function returns text trimmed to the specified witdh.</returns>
            public static string Compact(string text, Font font, int proposedWidth, EllipsisFormat options)
            {
                if (string.IsNullOrEmpty(text))
                    return text;

                // no alignment information
                if (((EllipsisFormat.Path | EllipsisFormat.Start | EllipsisFormat.End | EllipsisFormat.Middle) & options) == 0)
                    return text;

                if (font is null)
                    throw new ArgumentNullException(nameof(font));

                Size s = TextRenderer.MeasureText(text, font);

                // control is large enough to display the whole text
                if (s.Width <= proposedWidth)
                    return text;

                string pre = "";
                string mid = text;
                string post = "";

                bool isPath = (EllipsisFormat.Path & options) != 0;

                // split path string into <drive><directory><filename>
                if (isPath)
                {
                    pre = Path.GetPathRoot(text);
                    mid = Path.GetDirectoryName(text).Substring(pre.Length);
                    post = Path.GetFileName(text);
                }

                int len = 0;
                int seg = mid.Length;
                string fit = "";

                // find the longest string that fits into 
                // the control boundaries using bisection method
                while (seg > 1)
                {
                    seg -= seg / 2;

                    int left = len + seg;
                    int right = mid.Length;

                    if (left > right)
                        continue;

                    if ((EllipsisFormat.Middle & options) == EllipsisFormat.Middle)
                    {
                        right -= left / 2;
                        left -= left / 2;
                    }
                    else if ((EllipsisFormat.Start & options) != 0)
                    {
                        right -= left;
                        left = 0;
                    }

                    // trim at a word boundary using regular expressions
                    if ((EllipsisFormat.Word & options) != 0)
                    {
                        if ((EllipsisFormat.End & options) != 0)
                        {
                            left -= prevWord.Match(mid, 0, left).Length;
                        }
                        if ((EllipsisFormat.Start & options) != 0)
                        {
                            right += nextWord.Match(mid, right).Length;
                        }
                    }

                    // build and measure a candidate string with ellipsis
                    string tst = mid.Substring(0, left) + EllipsisChars + mid.Substring(right);

                    // restore path with <drive> and <filename>
                    if (isPath)
                    {
                        tst = Path.Combine(pre, tst, post);
                    }
                    s = TextRenderer.MeasureText(tst, font);

                    // candidate string fits into control boundaries, try a longer string
                    // stop when seg <= 1
                    if (s.Width <= proposedWidth)
                    {
                        len += seg;
                        fit = tst;
                    }
                }

                if (len == 0) // string can't fit into control
                {
                    // "path" mode is off, just return ellipsis characters
                    if (!isPath)
                        return EllipsisChars;

                    // <directory> is empty
                    if (mid.Length == 0)
                    {
                        // <drive> is empty, return compacted <filename>
                        if (pre.Length == 0)
                            return Compact(text, font, proposedWidth, options & ~EllipsisFormat.Path);

                        // we compare ellipsis and <drive> to get the shorter
                        string testEllipsis = Path.Combine(EllipsisChars, ".");
                        testEllipsis = testEllipsis.Substring(0, testEllipsis.Length - 1);
                        if (TextRenderer.MeasureText(testEllipsis, font).Width < TextRenderer.MeasureText(pre, font).Width)
                            pre = EllipsisChars;
                    }
                    else
                    {
                        // "C:\...\filename.ext" No need to check for drive, but generates extra check
                        if (pre.Length > 0)
                        {
                            fit = Path.Combine(pre, EllipsisChars, post);

                            s = TextRenderer.MeasureText(fit, font);

                            if (s.Width <= proposedWidth) return fit;
                        }

                        pre = EllipsisChars;
                    }

                    // Try "...\filename.ext" or "c:\filename.ext"
                    fit = Path.Combine(pre, post);

                    s = TextRenderer.MeasureText(fit, font);

                    // if still not fit then return "...\f...e.ext"
                    if (s.Width > proposedWidth)
                    {
                        fit = Path.Combine(pre, Compact(post, font, proposedWidth - TextRenderer.MeasureText(fit.Substring(0, fit.Length - post.Length), font).Width, options & ~EllipsisFormat.Path));
                    }
                }
                return fit;
            }
        }
    }

}
