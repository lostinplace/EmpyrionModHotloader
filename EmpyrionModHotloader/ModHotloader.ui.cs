using Eleon.Modding;
using EmpyrionMessageBroker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EmpyrionModHotloader
{
    public partial class ModHotloader : ModInterface
    {
        //TODO: Do the UI
        //TODO: Test to make it sure it works
        // Load permissions

        private Dictionary<int, string> playerSteamIds = new Dictionary<int, string>();
        

        private void HandleChatCommand(CmdId cmd, ChatInfo data)
        {
            if (cmd != CmdId.Event_ChatMessage) return;
            
            if (!playerSteamIds.ContainsKey(data.playerId))
            {
            
                new APIResult<PlayerInfo>().From(new Id(data.playerId)).OnResponse(x =>
                {
                 
                    playerSteamIds[data.playerId] = x.steamId;
                    HandleChatCommand(cmd, data);
                }).Execute();
                return;
            }
            
            var steamId = playerSteamIds[data.playerId];
            
            if (!elevated.Contains(steamId)) return;
            
            var action = getAction(data.msg);
            action(data);
        }

        private static Action<ChatInfo> defaultAction = (x) => { Broker.api.Console_Write("default!!"); };

        private static  Action<ChatInfo> getAction(string message)
        {
            foreach (var item in actions.Keys.ToList())
            {
                var re = new Regex(item);
                var match = re.Match(message);
                if (match.Success)
                {
                    return (x) => actions[item](x, match);
                }
            }
            return defaultAction;
        }


        private static Dictionary<string, Action<ChatInfo, Match>> actions = new Dictionary<string, Action<ChatInfo, Match>>()
        {
            { @"\\mod list", ListMods },
            { @"\\mod activate (.*)", ActivateMod },
            { @"\\mod deactivate (.*)", DeactivateMod },
            { @"\\mod flush (.*)", FlushMod }
        };

        private static void ListMods(ChatInfo info, Match match)
        {
            showTable(info.playerId);
        }

        private static void ActivateMod(ChatInfo info, Match match)
        {
            var mod = modManager.activateMod(match.Groups[1].Value);
            showTable(info.playerId, $"activated mod: {mod.title}");
        }
        private static void DeactivateMod(ChatInfo info, Match match)
        {
            var mod = modManager.deactivateMod(match.Groups[1].Value);
            showTable(info.playerId, $"deactivated mod: {mod.title}");
        }

        private static void FlushMod(ChatInfo info, Match match)
        {
            var mod = modManager.FlushMod(match.Groups[1].Value);
            showTable(info.playerId, $"flushed mod: {mod.title}");
        }

        private static void showTable(int playerId, string message ="")
        {
            var table = modManager.getModTable();

            var outMsg = message != "" ? $"{message}\n***\n{table}" : table;

            var msg = new IdMsgPrio()
            {
                id = playerId,
                msg = outMsg,
            };

            Broker.api.Console_Write("list!!");
            new GenericAPICommand(CmdId.Request_ShowDialog_SinglePlayer, msg).Execute();
        }

       
    }
}
