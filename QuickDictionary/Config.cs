using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickDictionary
{
    public class Config
    {
        public List<string> SelectedDictionaries { get; set; } = new List<string>();
        public bool AlwaysOnTop { get; set; } = false;
        public bool PauseClipboard { get; set; } = false;
        public string LastWordListName { get; set; } = null;
    }
}
