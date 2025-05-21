using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGMakerUtils.Models
{
    [AddINotifyPropertyChangedInterface]
    internal class GameDataFile
    {
        public string FilePath { get; set; } = string.Empty;

        public string FileName => Path.GetFileName(FilePath);

        public bool IsDone { get; set; } = false;
    }
}
