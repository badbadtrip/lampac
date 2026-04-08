using Shared.Models.Base;
using System;

namespace Collaps
{
    public class ModuleConf : BaseSettings, ICloneable
    {
        public ModuleConf(string plugin, string host, bool enable = true, bool streamproxy = false, bool two = false)
        {
            this.enable = enable;
            this.plugin = plugin;
            this.streamproxy = streamproxy;
            this.two = two;

            if (host != null)
                this.host = host.StartsWith("http") ? host : Decrypt(host);
        }


        public bool two { get; set; }

        public bool dash { get; set; }


        public ModuleConf Clone()
        {
            return (ModuleConf)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
    }
}
