using System.Collections.Generic;

namespace Lamson.Models.KinoGram
{
    public class Serial
    {
        public string id { get; set; }

        public List<(string link, string quality)> links { get; set; }
    }
}
