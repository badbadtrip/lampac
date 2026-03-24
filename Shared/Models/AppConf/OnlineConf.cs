using Shared.Models.Module;

namespace Shared.Models.AppConf
{
    public class OnlineConf : ModuleBaseConf
    {
        public string name { get; set; }

        public bool version { get; set; }

        public bool btn_priority_forced { get; set; }
    }
}
