using Eleon.Modding;
using EmpyrionMessageBroker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmpyrionModHotloader
{
    public partial class ModHotloader : ModInterface
    {
        private static ModManager modManager;
        private static AdminConfig adminConfig = AdminConfig.FromGameConfig();
        private static HashSet<string> elevated;

        void ModInterface.Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            Broker.HandleGameEvent(eventId, seqNr, data);
            if (eventId == CmdId.Event_ChatMessage) HandleChatCommand(eventId, (ChatInfo)data);
            modManager.Handle_Game_Event(eventId, seqNr, data);
        }

        void ModInterface.Game_Exit()
        {
            modManager.Handle_Game_Exit();
        }

        private ModGameAPI api;

        void ModInterface.Game_Start(ModGameAPI dediAPI)
        {
            Broker.api = dediAPI;
            elevated = new HashSet<string>(adminConfig.Elevated.Select(x => x.Id));
            api = dediAPI;
            dediAPI.Console_Write(adminConfig.Elevated.First().Id);
            modManager = new ModManager("Content/Mods/Hotloader/watched", dediAPI, verbose:false);
        }

        void ModInterface.Game_Update()
        {
            modManager.Handle_Game_Update();
        }
    }
}
