using System.Collections.Generic;

namespace Lamson.Models.KinoGram
{
    public class Voice
    {
        public string id { get; set; }

        public string name { get; set; }

        public List<Serial> episodes { get; set; }
    }
}
