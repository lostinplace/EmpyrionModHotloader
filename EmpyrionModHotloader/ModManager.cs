using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EmpyrionModHotloader
{
    public class ModManager
    {
        public struct ModItem
        {
            public string id;
            public string title;
            public string path;
            public ModInterface mod;
            public bool isActive;
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
            cadidatePaths.ForEach(x=>OnboardMod(x, true));

            var modTable = getModTable(modDict.Values.ToList());
            api.Console_Write("\n" + modTable);

            var path = Path.GetFullPath(folderPath);
            watcher = new FileSystemWatcher()
            {
                Path = folderPath,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };
            api.Console_Write($"*** HotloaderMod now watching {path}");
            watcher.IncludeSubdirectories = true;
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;
        }

        public static string getHashSha256(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            log(() => "detected rename");
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

        public string getModTable()
        {
            return getModTable(this.modDict.Values.ToList());
        }

        public static string getModTable(List<ModItem> mods)
        {
            var output = getModTableHeader();
            output += mods.Select(getModItemRow).Aggregate((x,y)=>x+"\n"+y);
            return output;
        }

        private static string getModTableHeader()
        {
            var headerRow = String.Format(columnFormat, new string[] { "id", "filename", "title", "state" });
            var headerBreak = new string('-', headerRow.Length);
            return  headerRow + "\n" + headerBreak + "\n" ;
        }

        private static string columnFormat = @"{0,-5}|{1,-35}|{2,-20}|{3,-10}";

        private static string getModItemRow(ModItem item)
        {
            
            var truncatedId = item.id.Substring(0, 5);
            var truncatedTitle = item.title.PadRight(20).Substring(0, 20);
            var truncatedFilename = Path.GetFileName(item.path).PadRight(35).Substring(0, 35);
            var state = item.isActive ? "active" : "inactive";
            return String.Format(columnFormat, new string[] { truncatedId, truncatedFilename, truncatedTitle, state });
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
            modDict.Values.Where(x=>x.isActive).ToList().ForEach(x => {
                log(() => $"sending msg {seqNr} to {x.title}");
                x.mod.Game_Event(evt, seqNr, data);
            });
        }


        public ModItem activateMod(string id)
        {
            var mod = modDict.Values.Where(x => x.id.StartsWith(id) && !x.isActive).First();
            var newMod = StartMod(mod);
            modDict[mod.path] = newMod;
            return newMod;
        }

        public ModItem deactivateMod(string id)
        {
            var mod = modDict.Values.Where(x => x.id.StartsWith(id) && x.isActive).First();
            var newMod = StopMod(mod);
            modDict[mod.path] = newMod;
            return newMod;
        }

        public ModItem StopMod(ModItem mod)
        {
            api.Console_Write($"ModHotLoader is stopping mod: {mod.title} from {mod.path}");
            mod.mod.Game_Exit();
            mod.isActive = false;
            api.Console_Write($"ModHotLoader has finished stopping mod: {mod.title}");
            return mod;
        }

        public ModItem FlushMod(string id)
        {
            var mod = modDict.Values.Where(x => x.id.StartsWith(id)).First();
            var active = mod.isActive;

            if(active) deactivateMod(mod.id);
            var newMod = loadMod(mod.path).mod;
            mod.mod = newMod;
            if (active) activateMod(mod.id);
            return mod;
        }

        public void Handle_Game_Update()
        {
            modDict.Values.Where(x=>x.isActive).ToList().ForEach(x => {
                log(() => $"sending update to {x.title}");
                x.mod.Game_Update();
            });
        }

        public void Handle_Game_Exit()
        {
            modDict.Values.Where(x=>x.isActive).ToList().ForEach(UnloadMod);
        }

        public void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            log(() => "detected delete");
            if (modDict.ContainsKey(e.FullPath))
            {
                var mod = modDict[e.FullPath];
                api.Console_Write($"Detected deletion of registered mod @\"{e.FullPath}\", unloading mod: {mod.title}");
                UnloadMod(mod);
            }
        }

        public void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            log(() => "detected create");
            api.Console_Write($"New dll detected at \"{e.FullPath}\", attempting to load mod");
            OnboardMod(e.FullPath);
        }

        public void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            log(() => "detected change");
            if (modDict.ContainsKey(e.FullPath))
            {
                var oldMod = modDict[e.FullPath];
                api.Console_Write($"Change Detected for mod {oldMod.title}@{oldMod.path}");
                UnloadMod(oldMod);
                OnboardMod(e.FullPath);
            }
        }

        public void OnboardMod(string path, bool autoStart=false)
        {
            try
            {
                var newMod = loadMod(path);
                modDict[newMod.path] = newMod;
                if (autoStart && !offlinePattern.IsMatch(newMod.path))
                {
                    modDict[newMod.path] = StartMod(newMod);
                }
            }
            catch (Exception ex)
            {
                api.Console_Write($"ModHotloader encountered an exception while attempting to load a mod from {path}");
                api.Console_Write(ex.ToString());
            }
        }

        public ModItem StartMod(ModItem modItem)
        {
            api.Console_Write($"ModHotLoader is starting mod: {modItem.title} from {modItem.path}");
            modItem.mod.Game_Start(api);
            modItem.isActive = true;
            api.Console_Write($"ModHotLoader has finished starting mod: {modItem.title}");
            return modItem;
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
                    id = getHashSha256(title + path),
                    title = title,
                    path = path,
                    mod = mod,
                    isActive = false
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
