using Rage.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadMusicianGTATools
{
    internal static class ConsoleCommands
    {
        [ConsoleCommand("Connects using token from your clipboard. Obtain token on https://badmusician.ru/gta/")]
        private static void BadMusicianGTATools_Connect()
        {
            Plugin.Connect(Rage.Game.GetClipboardText());
        }

        [ConsoleCommand("Disconnects")]
        private static void BadMusicianGTATools_Disconnect()
        {
            Plugin.Disconnect();
        }
    }
}
