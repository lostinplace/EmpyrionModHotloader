using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DummyMod
{
    public class DummyMod : ModInterface
    {

        private ModGameAPI api;

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            api.Console_Write("dummy 1 event");
            api.Console_Write(data.ToString());
        }

        public void Game_Exit()
        {
            api.Console_Write("exiting dummy 1");
        }

        public void Game_Start(ModGameAPI dediAPI)
        {
            api = dediAPI;
            var currentDirectory = Directory.GetCurrentDirectory();

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            var assemblyDir = Path.GetDirectoryName(path);

            api.Console_Write($"starting dummy 1 from directory: {currentDirectory}");
            api.Console_Write($"launched from assembly in directory: {assemblyDir}");
        }

        public void Game_Update()
        {
            api.Console_Write("updating dummy 1");
        }
    }

    public class OtherClass { }

    public static class AStaticClass { }

}
