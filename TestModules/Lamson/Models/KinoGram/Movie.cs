using System.Collections.Generic;

namespace Lamson.Models.KinoGram
{
    public class Movie
    {
        public string translation { get; set; }

        public List<(string link, string quality)> links { get; set; }
    }
}
