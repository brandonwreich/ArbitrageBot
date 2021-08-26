using ArbitrageBot.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ArbitrageBot
{
    class GatherInformation
    {
        public List<Listing> GrabBookInfo(string text)
        {
            //Initialize variables
            Regex regex = new Regex(@"\[{.*}\]");

            //Grab the wanted string
            var info = regex.Match(text).ToString();

            //Convert to Json object
            var listings = JsonConvert.DeserializeObject<List<Listing>>(info);

            return listings;
        }
    }
}
