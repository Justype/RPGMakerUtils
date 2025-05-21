using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGMakerUtils.Models
{
    internal class TranslateTarget
    {
        public string Name { get; set; }

        public JTokenType TokenType { get; set; }

        public string[] SubTargets { get; set; }

        public TranslateTarget(string name)
        {
            Name = name;
            TokenType = JTokenType.String;
            SubTargets = null;
        }

        public TranslateTarget(string name, string[] subTargets)
        {
            Name = name;
            TokenType = JTokenType.Array;
            SubTargets = subTargets;
        }

        public TranslateTarget(string name, JTokenType tokenType, string[] subTargets)
        {
            Name = name;
            TokenType = tokenType;
            SubTargets = subTargets;
        }
    }
}
