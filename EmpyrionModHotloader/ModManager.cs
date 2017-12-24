using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EmpyrionModHotloader
{
    public class ModManager
    {
        public struct ModItem
        {
            public string title;
            public string path;
            public ModInterface mod;
        }

        private ModGameAPI api;

        private static Type interfaceType = typeof(ModInterface);
        private FileSystemWatcher watcher;

        private Dictionary<string, ModItem> modDict = new Dictionary<string, ModItem>();
        private bool verbose;
        private Regex offlinePattern = new Regex(@"\.offline\.dll$");

        public ModManager(string folderPath, ModGameAPI api, Regex offlinePattern=null, bool verbose = false)
        {
            this.api = api;
            this.verbose = verbose;
            if(offlinePattern != null) this.offlinePattern = offlinePattern;
            var cadidatePaths = getCandidateModPaths(folderPath);
            var mods = getModsFromPaths(cadidatePaths);
            var path = Path.GetFullPath(folderPath);
            modDict = mods.ToDictionary(x => x.path);
            mods.ForEach(StartMod);
            watcher = new FileSystemWatcher()
            {
                Path = folderPath,
                IncludeSubdirectories = true,
                Filter = "*.dll"
            };
            watcher.IncludeSubdirectories = true;
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (offlinePattern.IsMatch(e.OldFullPath) && !offlinePattern.IsMatch(e.FullPath)) OnboardMod(e.FullPath);            
            if (!modDict.ContainsKey(e.OldFullPath)) return;
            var mod = modDict[e.OldFullPath];
            api.Console_Write($"detected rename of mod: {mod.title}@\"{e.OldFullPath}\"");
            if (offlinePattern.IsMatch(e.FullPath))
            {
                api.Console_Write($"mod invalidated using pattern: \"{offlinePattern.ToString()}\"");
                UnloadMod(mod);
            }
            api.Console_Write($"mod record transferred from \"{e.OldFullPath}\" to \"{e.FullPath}\"");
            modDict.Remove(e.OldFullPath);
            modDict[e.FullPath] = mod;
        }

        private void log(string message)
        {
            if (verbose)
            {
                api.Console_Write(message);
            }
        }

        private void log(Func<string> msgFunc)
        {
            if (verbose)
            {
                api.Console_Write(msgFunc());
            }
        }

        public void Handle_Game_Event(CmdId evt, ushort seqNr, object data)
        {   
            modDict.Values.ToList().ForEach(x => {
                log(() => $"sending msg {seqNr} to {x.title}");
                x.mod.Game_Event(evt, seqNr, data);
            });
        }

        public void Handle_Game_Update()
        {
            modDict.Values.ToList().ForEach(x => {
                log(() => $"sending update to {x.title}");
                x.mod.Game_Update();
            });
        }

        public void Handle_Game_Exit()
        {
            modDict.Values.ToList().ForEach(UnloadMod);
        }

        public void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (modDict.ContainsKey(e.FullPath))
            {
                var mod = modDict[e.FullPath];
                api.Console_Write($"Detected deletion of registered mod @\"{e.FullPath}\", unloading mod: {mod.title}");
                UnloadMod(mod);
            }
        }

        public void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            api.Console_Write($"New dll detected at \"{e.FullPath}\", attempting to load mod");
            OnboardMod(e.FullPath);
        }

        public void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (modDict.ContainsKey(e.FullPath))
            {
                var oldMod = modDict[e.FullPath];
                api.Console_Write($"Change Detected for mod {oldMod.title}@{oldMod.path}");
                UnloadMod(oldMod);
                OnboardMod(e.FullPath);
            }
        }

        public void OnboardMod(string path)
        {
            try
            {
                var newMod = loadMod(path);
                modDict[path] = newMod;
                StartMod(newMod);
            }
            catch (Exception ex)
            {
                api.Console_Write($"ModHotloader encountered an exception while attempting to load a mod from {path}");
                api.Console_Write(ex.ToString());
            }
        }

        public void StartMod(ModItem modItem)
        {
            api.Console_Write($"ModHotLoader is starting mod: {modItem.title} from {modItem.path}");
            modItem.mod.Game_Start(api);
            api.Console_Write($"ModHotLoader has finished starting mod: {modItem.title}");
        }

        public void UnloadMod(string pathOrTitle)
        {
            if (modDict.ContainsKey(pathOrTitle))
            {
                UnloadMod(modDict[pathOrTitle]);
                return;
            }
            var applicableMods = modDict.Values.ToList().Where(x => x.title == pathOrTitle);
            applicableMods.ToList().ForEach(UnloadMod);
        }

        public void UnloadMod(ModItem modItem)
        {
            api.Console_Write($"ModHotLoader is unloading mod: {modItem.title} from {modItem.path}");
            modItem.mod.Game_Exit();
            modDict.Remove(modItem.path);
            api.Console_Write($"ModHotLoader hasfinished unloading mod: {modItem.title}");
        }

        public static ModItem loadMod(string modPath)
        {
            
            var path = Path.GetFullPath(modPath);
            var DLL = Assembly.LoadFile(path);
            var types = DLL.GetExportedTypes();
            var modTypes = types.Where(x => x.GetInterfaces().Contains(interfaceType)).ToList();
            if (modTypes.Count > 0)
            {
                var title = getModTitle(DLL);
                var mod = (ModInterface)Activator.CreateInstance(modTypes.First());
                return new ModItem()
                {
                    title = title,
                    path = path,
                    mod = mod
                };
            }
            return default(ModItem);            
        }

        private static string getModTitle(Assembly anAssembly)
        {
            var attributes = anAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var assemblyTitleAttribute = attributes.SingleOrDefault() as AssemblyTitleAttribute;
            return assemblyTitleAttribute.Title;
        }

        public static List<string> getCandidateModPaths(string aPath)
        {
            var path = Path.GetFullPath(aPath);
            var result = Directory.GetFiles(aPath, @"*.dll", SearchOption.AllDirectories);
            return result.ToList();
        }

        public static List<ModItem> getModsFromPaths(List<string> paths)
        {
            var result = paths
                .Select(loadMod)
                .Where(x => !Equals(x, default(ModItem)));
            return result.ToList();
        }
    }
}
