using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGMakerUtils.Messages
{
    internal class TranslateJsonUpdatedMessage : ValueChangedMessage<string>
    {
        public TranslateJsonUpdatedMessage(string value) : base(value)
        {
        }
    }
}
