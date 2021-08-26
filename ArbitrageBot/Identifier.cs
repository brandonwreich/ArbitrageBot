using ArbitrageBot.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ArbitrageBot
{
    class Identifier
    {
        public static async Task GetBookDetails(List<Listing> listings)
        {
            #region Initalizations

            //Initialize lists
            List<string> cityList = new List<string> { "Sandy", "Draper", "Millcreek", "Highland", "slc", "South Jordan",
                "Saratoga Springs", "Midvale", "S Jordan", "Alpine", "Cedar Hills", "Bluffdale", "West Jordan", "Salt Lake City",
                "Riverton", "Orem", "Provo", "American Fork" };
            List<Listing> abeBooksList = new List<Listing>();
            List<Listing> bookByteList = new List<Listing>();
            List<Listing> booksRunList = new List<Listing>();
            List<Listing> buyback101List = new List<Listing>();
            List<Listing> eCampusList = new List<Listing>();
            List<Listing> myBookCartList = new List<Listing>();
            List<Listing> sellBackYourBookList = new List<Listing>();
            List<Listing> textbookManiacList = new List<Listing>();
            List<Listing> textbooksRUsList = new List<Listing>();
            List<Listing> ziffitList = new List<Listing>();
            List<Listing> errorList = new List<Listing>();

            //Initialize variables
            decimal abeBookListProft = 0;
            decimal bookByteListProfit = 0;
            decimal booksRunListProfit = 0;
            decimal buyBack101ListProfit = 0;
            decimal eCampusListProfit = 0;
            decimal myBookCartListProfit = 0;
            decimal sellBackYourBookListProfit = 0;
            decimal textbookManiacListProfit = 0;
            decimal textbooksRUsListProfit = 0;
            decimal ziffitListProfit = 0;
            bool exit = false;

            #endregion

            //Loop through listings
            foreach (var listing in listings)
            {
                try
                {
                    //Loop through cities
                    foreach (string city in cityList)
                    {
                        //Initialize variables
                        var town = listing.City ?? "Unknown";

                        //If book is in a surrounding city
                        if (town.Equals(city, StringComparison.InvariantCultureIgnoreCase) && listing.Price > 5)
                        {
                            //Initialize variables
                            var httpClient = new HttpClient();
                            var encoded = HttpUtility.UrlEncode(listing.Title);
                            var response = await httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes?q={encoded}");
                            dynamic levels = null;
                            listing.HighestOffer = 0;

                            /*
                             * Finds the isbn number of the book in the listing gathered
                             */
                            #region Find isbn number

                            //If API is reached
                            if (response.IsSuccessStatusCode)
                            {
                                //Initialize variables
                                var jsonString = await response.Content.ReadAsStringAsync();

                                //Parse the json
                                JObject jsonVal = JObject.Parse(jsonString);
                                levels = jsonVal;
                            }
                            else
                            {
                                //Report failure
                                Console.WriteLine(response.StatusCode);
                            }

                            //If levels isn't null
                            if (levels != null)
                            {
                                //Initialize variables
                                bool isbn13Exists = false;
                                bool isbn10Exists = false;
                                int attemptCount = 0;
                                var isbn13 = "";
                                var isbn10 = "";

                                //While isbn 10 or 13 hasn't been found
                                while (isbn13Exists == false || isbn10Exists == false)
                                {
                                    //Initialize variables
                                    var type = "";

                                    //Find isbn strings
                                    type = levels.items[0].volumeInfo.industryIdentifiers[attemptCount].type;

                                    //If found the isbn 13 string
                                    if (type == "ISBN_13")
                                    {
                                        //Assign variables
                                        isbn13 = levels.items[0].volumeInfo.industryIdentifiers[attemptCount].identifier;

                                        isbn13Exists = true;
                                    }

                                    //If found the isbn 10 string
                                    if (type == ("ISBN_10"))
                                    {
                                        //Assign variables
                                        isbn10 = levels.items[0].volumeInfo.industryIdentifiers[attemptCount].identifier;

                                        isbn10Exists = true;
                                    }

                                    //If tried 5 times
                                    if (attemptCount == 4)
                                    {
                                        //Move on
                                        isbn13Exists = true;
                                        isbn10Exists = true;
                                    }

                                    //Increase count
                                    attemptCount++;
                                }

                                //If isbn13 isn't mull
                                if (Convert.ToString(isbn13) != null || Convert.ToString(isbn13) != "")
                                {
                                    //Assign Isbn 13
                                    listing.Isbn13 = isbn13;
                                }

                                //If isbn10 isn't null
                                if (Convert.ToString(isbn10) != null || Convert.ToString(isbn10) != "")
                                {
                                    //Assign Isbn 10
                                    listing.Isbn = isbn10;
                                }
                            }

                            #endregion

                            /*
                             * Take the isbn number found above and searches for the best buyback offer and store
                             * name
                             */
                            #region Find buyback offers

                            //Initialize variables
                            var isbn = listing.Isbn13 ?? listing.Isbn;

                            //If there is an isbn
                            if (isbn != null && isbn != "")
                            {
                                //Initialize variables
                                isbn = isbn.Replace("-", "");
                                httpClient.DefaultRequestHeaders.Add("authority", "www.bookfinder.com");
                                var response3 = await httpClient.GetAsync($"https://www.bookfinder.com/buyback/affiliate/{isbn}.mhtml");
                                var raw3 = await response3.Content.ReadAsStringAsync();

                                //If the API responds
                                if (response3.IsSuccessStatusCode)
                                {
                                    //Initialize variables
                                    var jobject = JObject.Parse(raw3);
                                    listing.OfferBookTitle = jobject.GetValue("title").ToString();
                                    var offers = jobject.GetValue("offers");
                                    var test1 = offers.Children();

                                    //Loop through offers
                                    foreach (var child in offers.Children().ToList())
                                    {
                                        //Initialize variables
                                        var test = child.Children().FirstOrDefault();

                                        //Search for highest offer
                                        if (test.Value<decimal>("buyback") == 1 && test.Value<decimal>("offer") > listing.HighestOffer)
                                        {
                                            //Assign the hieghest offer
                                            listing.HighestOffer = test.Value<decimal>("offer");
                                            listing.HighestOfferName = child.Path;
                                        }
                                    }
                                }
                            }

                            #endregion

                            /*
                             * Checks to see which store is offering the highest offer and adds it to the appropiate
                             * list and increments that list's total profit.
                             */
                            #region Add to lists

                            if (listing.HighestOffer > listing.Price)
                            {
                                //Alerts of potiential profit
                                Console.Beep();

                                if (listing.HighestOfferName.Equals("offers.abebooks_bb"))
                                {
                                    abeBooksList.Add(listing);

                                    abeBookListProft += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.bookbyte_bb"))
                                {
                                    bookByteList.Add(listing);

                                    bookByteListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.booksrun_bb"))
                                {
                                    booksRunList.Add(listing);

                                    booksRunListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.buyback101_bb"))
                                {
                                    buyback101List.Add(listing);

                                    buyBack101ListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.ecampus_bb"))
                                {
                                    eCampusList.Add(listing);

                                    eCampusListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.mybookcart_bb"))
                                {
                                    myBookCartList.Add(listing);

                                    myBookCartListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.sellbackyourbook_bb"))
                                {
                                    sellBackYourBookList.Add(listing);

                                    sellBackYourBookListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.textbookmaniac_bb"))
                                {
                                    textbookManiacList.Add(listing);

                                    textbookManiacListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.textbooksrus_bb"))
                                {
                                    textbooksRUsList.Add(listing);

                                    textbooksRUsListProfit += listing.HighestOffer - listing.Price;
                                }
                                else if (listing.HighestOfferName.Equals("offers.ziffit_bb"))
                                {
                                    ziffitList.Add(listing);

                                    ziffitListProfit += listing.HighestOffer - listing.Price;
                                }
                                else
                                {
                                    errorList.Add(listing);
                                }
                            }

                            #endregion

                            //Exit the loop
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    errorList.Add(listing);
                }
            }

            /*
             * Writes the list names and how much profit each one has to the console
             */
            #region Write Lists

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Abe Books: " + abeBookListProft);
            Console.WriteLine("Bookbyte: " + bookByteListProfit);
            Console.WriteLine("Books Run: " + booksRunListProfit);
            Console.WriteLine("Buyback101.com: " + buyBack101ListProfit);
            Console.WriteLine("eCampus.com: " + eCampusListProfit);
            Console.WriteLine("Mybookcart.com: " + myBookCartListProfit);
            Console.WriteLine("Sell Back Your Book: " + sellBackYourBookListProfit);
            Console.WriteLine("Textbook Maniac: " + textbookManiacListProfit);
            Console.WriteLine("Textbooks-R-Us: " + textbooksRUsListProfit);
            Console.WriteLine("Ziffit.com: " + ziffitListProfit);

            #endregion

            //Asks user which list they'd like printed
            Console.WriteLine("");
            Console.WriteLine("Which list would you like to print? Seperate answers with commas");

            while (!exit)
            {
                //Reads the response and parses the string
                var answer = Console.ReadLine();
                string[] wantedLists = answer.Split(",");

                #region Print the booklists or exit

                //Loop through the stores listed in the anser
                foreach (var list in wantedLists)
                {
                    //If the wanted store matches an available store
                    if (list.Equals("abe books", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Abe Books:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in abeBooksList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("bookbyte", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Bookbyte:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in bookByteList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("books run", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Books Run:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in booksRunList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("buyback101.com", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Buyback101.com:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in buyback101List)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("ecampus.com", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("eCampus.com:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in eCampusList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("mybookcart.com", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Mybookcart.com:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in myBookCartList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("sell back your book", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Sell Back Your Book:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in sellBackYourBookList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("textbook maniac", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Textbook Maniac");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in textbookManiacList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("textbooks-r-us", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Testbooks-R-Us:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in textbooksRUsList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("ziffit.com", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Ziffit.com:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in ziffitList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("error list", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Error List:");
                        Console.WriteLine("");

                        // Print each book in the list
                        foreach (var book in errorList)
                        {
                            //Build result
                            string result = $"Listing Title: {book.Title}" +
                                $"\n Found Book Title: {book.FoundBookTitle}" +
                                $"\n \t Isbn: {book.Isbn}" +
                                $"\n \t Isbn-13: {book.Isbn13}" +
                                $"\n \t Listing ID: {book.Id}" +
                                $"\n \t City: {book.City}" +
                                $"\n \t Price: ${book.Price}" +
                                $"\n \t Offer: ${book.HighestOffer}" +
                                $"\n \t Profit: ${book.HighestOffer - book.Price}";

                            //Write result
                            Console.WriteLine(result);
                        }
                    }
                    else if (list.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        exit = true;
                        Environment.Exit(0);
                    }

                }

                #endregion

                //Clear the list
                Array.Clear(wantedLists, 0, wantedLists.Length);

                //Reask for lists
                Console.WriteLine("Is there any lists you'd like to see? Please seperate with commas.");
                Console.WriteLine("");
            }
        }
    }
}