using ArbitrageBot.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArbitrageBot
{
    class Program
    {
        static void Main(string[] args)
        {
            //Initialize lists
            List<Listing> pageListings = new List<Listing>();
            List<string> websites = new List<string> { "https://classifieds.ksl.com/search/Books-and-Media/Books-Education-and-College/page/",
                "https://classifieds.ksl.com/search/Books-and-Media/Books-Religious/page/",
                "https://classifieds.ksl.com/search/Books-and-Media/Books-Children/page/",
                "https://classifieds.ksl.com/search/Books-and-Media/Books-Non-fiction/page/",
                "https://classifieds.ksl.com/search/Books-and-Media/Books-Fiction/page/" };

            //Initialize pages
            GatherInformation infoGrabbingPage = new GatherInformation();

            //Loop through website list
            foreach (string website in websites)
            {
                //Update console
                Console.WriteLine("Grabbing data...");

                //Initialize variables
                int pageNumber = 0;
                var url = website + pageNumber;
                var web = new HtmlWeb();
                var doc = web.Load(url);
                int maxPages = FindMaxPageNumber(doc);

                //While there are still listings
                while (pageNumber < maxPages)
                {
                    try
                    {
                        //Initialize variables
                        url = website + pageNumber;
                        web = new HtmlWeb();
                        doc = web.Load(url);

                        //Find listing information
                        var value = doc.DocumentNode.SelectNodes("//script[contains(.,'window.renderSearchSection')]").First().GetDirectInnerText();
                        value = value.Substring(value.IndexOf("listings: ") + 10);

                        //Add listings to the page list
                        pageListings.AddRange(infoGrabbingPage.GrabBookInfo(value));
                    }
                    catch (Exception x)
                    {
                        //Update console
                        Console.WriteLine("Failure to grab data on page " + pageNumber + ". " + x.Message);
                    }

                    //Increment page number count
                    pageNumber++;
                }
            }

            //Compare book prices
            Console.WriteLine("");
            Console.WriteLine("Comparing pricing...");
            Console.WriteLine("");
            Task.WaitAll(Identifier.GetBookDetails(pageListings));
        }

        //Helper method to find max number of pages
        public static int FindMaxPageNumber(HtmlDocument doc)
        {
            //Find max number of pages
            var value = doc.DocumentNode.SelectNodes("//*[@title='Go to last page']").First().GetDirectInnerText();

            return Convert.ToInt32(value);
        }
    }
}