using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DummyMod2
{
    public class DummyMod2 : ModInterface
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
            api.Console_Write("starting dummy 1");
        }

        public void Game_Update()
        {
            api.Console_Write("updating dummy 1");
        }
    }
}
