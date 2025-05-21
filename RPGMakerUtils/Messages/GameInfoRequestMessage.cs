using CommunityToolkit.Mvvm.Messaging.Messages;
using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGMakerUtils.Messages
{
    internal class GameInfoRequestMessage : RequestMessage<(string GamePath, RPGMakerVersion GameVersion)>
    {
    }
}
