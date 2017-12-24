using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmpyrionModHotloader
{
    public partial class ModHotloader : ModInterface
    {
        private ModManager modManager;

        void ModInterface.Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            modManager.Handle_Game_Event(eventId, seqNr, data);
        }

        void ModInterface.Game_Exit()
        {
            modManager.Handle_Game_Exit();
        }

        private ModGameAPI api;

        void ModInterface.Game_Start(ModGameAPI dediAPI)
        {
            api = dediAPI;
            modManager = new ModManager("Content/Mods/Hotloader/watched", dediAPI);
        }

        void ModInterface.Game_Update()
        {
            modManager.Handle_Game_Update();
        }
    }
}
