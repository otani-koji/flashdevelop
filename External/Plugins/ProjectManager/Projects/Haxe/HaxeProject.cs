using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using PluginCore;
using PluginCore.Helpers;

namespace ProjectManager.Projects.Haxe
{
    public class HaxeProject : Project
    {
        // hack : we cannot reference settings HaxeProject is also used by FDBuild
        public static bool saveHXML = false;

        protected string[] rawHXML;

        public HaxeProject(string path) : base(path, new HaxeOptions())
        {
            movieOptions = new HaxeMovieOptions();
        }

        public override string Language => "haxe";
        public override string LanguageDisplayName => "Haxe";
        public override bool IsCompilable => true;
        public override bool ReadOnly => false;
        public override bool HasLibraries => OutputType == OutputType.Application && IsFlashOutput;
        public override bool RequireLibrary => IsFlashOutput;
        public override string DefaultSearchFilter => "*.hx;*.hxp";

        public override string LibrarySWFPath
        {
            get
            {
                var projectName = RemoveDiacritics(Name);
                return Path.Combine("obj", projectName + "Resources.swf");
            }
        }

        public string[] RawHXML
        {
            get => rawHXML;
            set => ParseHXML(value);
        }

        public new HaxeOptions CompilerOptions => (HaxeOptions)base.CompilerOptions;

        public string HaxeTarget => MovieOptions.HasPlatformSupport ? MovieOptions.PlatformSupport.HaxeTarget : null;

        public bool IsFlashOutput => HaxeTarget == "swf";

        public override string GetInsertFileText(string inFile, string path, string export, string nodeType)
        {
            if (export != null) return export;
            var isInjectionTarget = (UsesInjection && path == GetAbsolutePath(InputPath));
            if (IsLibraryAsset(path) && !isInjectionTarget)
                return GetAsset(path).ID;

            var dirName = inFile;
            if (FileInspector.IsHaxeFile(inFile, Path.GetExtension(inFile).ToLower()))
                dirName = ProjectPath;

            return '"' + ProjectPaths.GetRelativePath(Path.GetDirectoryName(dirName), path).Replace('\\', '/') + '"'; 
        }

        public override CompileTargetType AllowCompileTarget(string path, bool isDirectory)
        {
            if (isDirectory || !FileInspector.IsHaxeFile(path, Path.GetExtension(path))) return CompileTargetType.None;

            foreach (string cp in AbsoluteClasspaths)
                if (path.StartsWith(cp, StringComparison.OrdinalIgnoreCase))
                    return CompileTargetType.AlwaysCompile | CompileTargetType.DocumentClass;
            return CompileTargetType.None;
        }

        public override bool IsDocumentClass(string path) 
        {
            foreach (string cp in AbsoluteClasspaths)
                if (path.StartsWith(cp, StringComparison.OrdinalIgnoreCase))
                {
                    string cname = GetClassName(path, cp);
                    if (CompilerOptions.MainClass == cname) return true;
                }
            return false;
        }

        public override void SetDocumentClass(string path, bool isMain)
        {
            if (isMain)
            {
                ClearDocumentClass();
                if (!IsCompileTarget(path)) SetCompileTarget(path, true);
                foreach (string cp in AbsoluteClasspaths)
                    if (path.StartsWith(cp, StringComparison.OrdinalIgnoreCase))
                    {
                        CompilerOptions.MainClass = GetClassName(path, cp);
                        break;
                    }
            }
            else 
            {
                SetCompileTarget(path, false);
                CompilerOptions.MainClass = "";
            }
        }

        private void ClearDocumentClass()
        {
            if (string.IsNullOrEmpty(CompilerOptions.MainClass)) 
                return;

            string docFile = CompilerOptions.MainClass.Replace('.', Path.DirectorySeparatorChar) + ".hx";
            CompilerOptions.MainClass = "";
            foreach (string cp in AbsoluteClasspaths)
            {
                var path = Path.Combine(cp, docFile);
                if (File.Exists(path))
                {
                    SetCompileTarget(path, false);
                    break;
                }
            }
        }

        public override bool Clean()
        {
            try
            {
                if (!string.IsNullOrEmpty(OutputPath) && File.Exists(GetAbsolutePath(OutputPath)))
                {
                    if (MovieOptions.HasPlatformSupport && MovieOptions.PlatformSupport.ExternalToolchain is null)
                        File.Delete(GetAbsolutePath(OutputPath));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        string Quote(string s)
        {
            if (s.IndexOf(' ') >= 0)
                return "\"" + s + "\"";
            return s;
        }

        public string[] BuildHXML(string[] paths, string outfile, bool release)
        {
            var pr = new List<string>();
            var isFlash = IsFlashOutput;

            if (rawHXML != null)
            {
                pr.AddRange(rawHXML);
            }
            else
            {
                // SWC libraries
                if (isFlash)
                    foreach (LibraryAsset asset in LibraryAssets)
                    {
                        if (asset.IsSwc)
                            pr.Add("-swf-lib " + asset.Path);
                    }

                // libraries
                foreach (var lib in CompilerOptions.Libraries)
                    if (lib.Length > 0)
                    {
                        if (lib.Trim().StartsWith("-lib", StringComparison.Ordinal)) pr.Add(lib);
                        else pr.Add("-lib " + lib);
                    }

                // class paths
                var classPaths = paths.ToList();
                classPaths.AddRange(Classpaths);
                foreach (var cp in classPaths)
                {
                    var ccp = string.Join("/", cp.Split('\\'));
                    pr.Add("-cp " + Quote(ccp));
                }

                // compilation mode
                var mode = HaxeTarget;
                //throw new SystemException("Unknown mode");

                if (mode != null)
                {
                    outfile = string.Join("/", outfile.Split('\\'));
                    pr.Add("-" + mode + " " + Quote(outfile));
                }

                // flash options
                if (isFlash)
                {
                    var htmlColor = MovieOptions.Background.Substring(1);
                    if (htmlColor.Length > 0)
                        htmlColor = ":" + htmlColor;

                    pr.Add("-swf-header " + $"{MovieOptions.Width}:{MovieOptions.Height}:{MovieOptions.Fps}{htmlColor}");

                    if (!UsesInjection && LibraryAssets.Count > 0)
                        pr.Add("-swf-lib " + Quote(LibrarySWFPath));

                    if (CompilerOptions.FlashStrict)
                        pr.Add("--flash-strict");

                    // haxe compiler uses Flash version directly
                    var version = MovieOptions.Version;
                    if (version != null) pr.Add("-swf-version " + version);
                }

                // defines
                foreach (var def in CompilerOptions.Directives)
                    pr.Add("-D " + Quote(def));

                // add project files marked as "always compile"
                foreach (var relTarget in CompileTargets)
                {
                    var absTarget = GetAbsolutePath(relTarget);
                    // guess the class name from the file name
                    foreach (var cp in classPaths)
                        if (absTarget.StartsWith(cp, StringComparison.OrdinalIgnoreCase))
                        {
                            var className = GetClassName(absTarget, cp);
                            if (CompilerOptions.MainClass != className)
                                pr.Add(className);
                        }
                }

                // add main class
                if (!string.IsNullOrEmpty(CompilerOptions.MainClass))
                    pr.Add("-main " + CompilerOptions.MainClass);
                
                // extra options
                foreach (var opt in CompilerOptions.Additional)
                {
                    var p = opt.Trim();
                    if (p == "" || p[0] == '#') continue;
                    char[] space = { ' ' };
                    var parts = p.Split(space, 2);
                    if (parts.Length == 1) pr.Add(p);
                    else pr.Add(parts[0] + ' ' + Quote(parts[1]));
                }
            }

            // debug
            if (!release)
            {
                pr.Insert(0, "-debug");
                if (CurrentSDK is null || !CurrentSDK.Contains("Motion-Twin")) // Haxe 3+
                    pr.Insert(1, "--each");
                if (isFlash && EnableInteractiveDebugger && CompilerOptions.EnableDebug)
                {
                    pr.Insert(1, "-D fdb");
                    if (CompilerOptions.NoInlineOnDebug)
                        pr.Insert(2, "--no-inline");
                }
            }
            return pr.ToArray();
        }

        private string GetClassName(string absTarget, string cp)
        {
            var className = absTarget.Substring(cp.Length);
            className = className.Substring(0, className.LastIndexOf('.'));
            className = Regex.Replace(className, "[\\\\/]+", ".");
            if (className.StartsWith(".", StringComparison.Ordinal)) className = className.Substring(1);
            return className;
        }

        #region Load/Save

        public static HaxeProject Load(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            if (ext == ".hxml")
            {
                return new HaxeProject(path) {RawHXML = File.ReadAllLines(path)};
            }

            var reader = new HaxeProjectReader(path);

            try
            {
                return reader.ReadProject();
            }
            catch (XmlException e)
            {
                var format = $"Error in XML Document line {e.LineNumber}, position {e.LinePosition}.";
                throw new Exception(format, e);
            }
            finally { reader.Close(); }
        }

        public override void Save() => SaveAs(ProjectPath);

        public override void SaveAs(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            if (ext != ".hxproj") return;

            if (!AllowedSaving(fileName)) return;
            try
            {
                var writer = new HaxeProjectWriter(this, fileName);
                writer.WriteProject();
                writer.Flush();
                writer.Close();
                if (saveHXML && OutputType != OutputType.CustomBuild)
                {
                    var hxml = File.CreateText(Path.ChangeExtension(fileName, "hxml"));
                    foreach(var line in BuildHXML(new string[0], OutputPath,true))
                        hxml.WriteLine(line);
                    hxml.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IO Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region HXML parsing

        private void ParseHXML(string[] raw)
        {
            if (raw != null && (raw.Length == 0 || raw[0] is null))
                raw = null;
            rawHXML = raw;

            var libs = new List<string>();
            var defs = new List<string>();
            var cps = new List<string>();
            var add = new List<string>();
            var target = PlatformData.JAVASCRIPT_PLATFORM;
            var haxeTarget = "js";
            var output = "";
            if (raw != null) ParseHxmlEntries(raw, defs, cps, libs, add, ref target, ref haxeTarget, ref output, ".");

            CompilerOptions.Directives = defs.ToArray();
            CompilerOptions.Libraries = libs.ToArray();
            CompilerOptions.Additional = add.ToArray();
            if (cps.Count == 0) cps.Add(".");
            Classpaths.Clear();
            Classpaths.AddRange(cps);

            if (MovieOptions.HasPlatformSupport)
            {
                var platform = MovieOptions.PlatformSupport;
                MovieOptions.TargetBuildTypes = platform.Targets;

                if (platform.Name == "hxml" && string.IsNullOrEmpty(TargetBuild))
                    TargetBuild = haxeTarget ?? "";
            }
            else MovieOptions.TargetBuildTypes = null;

            if (MovieOptions.TargetBuildTypes is null)
            {
                OutputPath = output;
                OutputType = OutputType.Application;
                MovieOptions.Platform = target;
            }
        }

        private void ParseHxmlEntries(string[] lines, List<string> defs, List<string> cps, List<string> libs, List<string> add, ref string target, ref string haxeTarget, ref string output, string cwd)
        {
            var reHxOp = new Regex("^-([a-z0-9-]+)\\s*(.*)", RegexOptions.IgnoreCase);
            foreach (string line in lines)
            {
                if (line is null) break;
                var trimmedLine = line.Trim();
                var m = reHxOp.Match(trimmedLine);
                if (m.Success)
                {
                    var op = m.Groups[1].Value;
                    if (op == "-next")
                        break; // ignore the rest

                    var value = m.Groups[2].Value.Trim();
                    switch (op)
                    {
                        case "D": defs.Add(value); break;
                        case "cp": cps.Add(CleanPath(value, cwd)); break;
                        case "lib": libs.Add(value); break;
                        case "main": CompilerOptions.MainClass = value; break;
                        case "swf":
                        case "swf9":
                            target = PlatformData.FLASHPLAYER_PLATFORM;
                            haxeTarget = "flash";
                            output = value;
                            break;
                        case "swf-header":
                            var header = value.Split(':');
                            int.TryParse(header[0], out MovieOptions.Width);
                            int.TryParse(header[1], out MovieOptions.Height);
                            int.TryParse(header[2], out MovieOptions.Fps);
                            MovieOptions.Background = header[3];
                            break;
                        case "-connect": break; // ignore
                        case "-each": break; // ignore
                        case "-cwd":
                            cwd = CleanPath(value, cwd);
                            break;
                        default:
                            // detect platform (-cpp output, -js output, ...)
                            var targetPlatform = FindPlatform(op);
                            if (targetPlatform != null)
                            {
                                target = targetPlatform.Name;
                                haxeTarget = targetPlatform.HaxeTarget;
                                output = value;
                            }
                            else add.Add(line);
                            break;
                    }
                }
                else if (!trimmedLine.StartsWith("#") && trimmedLine.EndsWith(".hxml", StringComparison.OrdinalIgnoreCase))
                {
                    var subhxml = GetAbsolutePath(CleanPath(trimmedLine, cwd));
                    if (File.Exists(subhxml))
                    {
                        ParseHxmlEntries(File.ReadAllLines(subhxml), defs, cps, libs, add, ref target, ref haxeTarget, ref output, cwd);
                    }
                }
            }
        }

        private LanguagePlatform FindPlatform(string op)
        {
            var lang = PlatformData.SupportedLanguages["haxe"];
            foreach (var platform in lang.Platforms.Values)
            {
                if (platform.HaxeTarget == op) return platform;
            }
            return null;
        }

        private string CleanPath(string path, string cwd)
        {
            path = path.Replace("\"", string.Empty);
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            // handle if NME/OpenFL config file is not at the root of the project directory
            if (Path.IsPathRooted(path)) return path;
            
            var relDir = Path.GetDirectoryName(ProjectPath);
            var absPath = Path.GetFullPath(Path.Combine(relDir, cwd, path));
            return GetRelativePath(absPath);
        }
        #endregion
    }
}
