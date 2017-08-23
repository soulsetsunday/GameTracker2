﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
//using GameTracker2.Data;
using GameTracker2.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using GameTracker2.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Globalization;
using HtmlAgilityPack;
using GameTracker2.Classes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace GameTracker2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class SearchController : Controller
    {
        private GameDbContext context;
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _hostingEnvironment;

        public SearchController(GameDbContext dbContext, UserManager<ApplicationUser> userManager, IHostingEnvironment hostingEnvironment)
        {
            context = dbContext;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
        }

        private const string apiFileLocation = "/data/apikey.txt";
        private string privateapikey = LoadAPI();
        public const int mostRecentlyAddedLimit = 5;
        public static RootObject searchResults = new RootObject();
        //static to pass around controller, list of results from rootobject, this is still in use
        public static Platform tempPlatform = new Platform();
        public static int storeIndex;
        public static DateTime dateForDB;
        public static DateTime currentWorkingDate;
        //all 4 still in use, change these

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        [HttpGet]
        public IActionResult Index(string id = "0")
        {
            DateTime calendarDate;
            if (!DateTime.TryParseExact(id, "MM-dd-yyyy", DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out calendarDate))
            {
                calendarDate = DateTime.Today;
            }
            //if id can't be parsed, use today

            ViewBag.date = calendarDate;
            currentWorkingDate = calendarDate;


            IList<Game> games = context.Games.Where(u => u.User == GetCurrentUserAsync().Result).Include(c => c.Platform).Include(i => i.GameImages).OrderByDescending(x => x.MostRecentlyAdded).Take(mostRecentlyAddedLimit).ToList();
            return View(games);
        }

        [HttpPost]
        public IActionResult ReleaseDetails(string date, int gameid, int platformid)
        {
            //this assumes a non-parsable date is a url/all dates are valid and parsable
            DateTime dateParse = new DateTime();
            if (DateTime.TryParse(date, out dateParse))
                return AddGame(gameid, platformid, date);
            //if a valid date is passed, send everything to AddGame

            //if an invalid date is passed, hopefully it's a url(!); if so, append releases/ to it 
            // and scrape the page

            var html = date + "releases/";
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(html);

            List<string> pageValues = new List<string>() { "name", "platform", "region", "releaseDate" };
            List<string> releaseDates = new List<string>();
            //List<string> justDates = new List<string>();
            List<ViewDateDetail> detail = new List<ViewDateDetail>();

            var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//tbody/tr/td");

            foreach (var node in htmlNodes)
            {
                if (node.Attributes.Count > 0 && pageValues.Contains(node.Attributes["data-field"].Value))
                {
                    releaseDates.Add(node.Attributes["data-field"].Value.Trim());
                    releaseDates.Add(node.InnerText.Trim());
                }

            }

            for (int i = 0; i < releaseDates.Count; i++)
            {
                if ((i + 1) % 8 == 0)
                {
                    //justDates.Add(releaseDates[i]);
                    ViewDateDetail tempView = new ViewDateDetail();

                    tempView.ReleaseDetailDate = releaseDates[i];
                    tempView.ReleaseDetailPlatoform = releaseDates[i - 4];
                    tempView.ReleaseDetailRegion = releaseDates[i - 2];

                    detail.Add(tempView);

                }
            }

            ReleaseDetailsViewModel send = new ReleaseDetailsViewModel
            {
                GameID = gameid,
                PlatformID = platformid,
                //ReleaseDates = justDates,
                ViewDateDetails = detail,
            };

            return View(send);
        }

        [HttpPost]
        public async Task<IActionResult> Results(string searchstring, int page = 1)
        {
            ViewBag.Searchstring = searchstring;
            // Temporary thing
            //RootObject searchResults = LoadJson();

            //actual loading
            string response = await SendSearchRequest(searchstring, page);
            searchResults = JsonConvert.DeserializeObject<RootObject>(response);

            return View(searchResults);
        }

        [HttpPost]
        public IActionResult AddGame(int gameid, int platformid, string date)
        {

            for (int i = 0; i < searchResults.Results.Count; i++)
            {
                if (searchResults.Results[i].id == gameid)
                {
                    storeIndex = i;

                    for (int j = 0; j < searchResults.Results[i].Platforms.Count; j++)
                    {
                        if (searchResults.Results[i].Platforms[j].ID == platformid)
                        {
                            
                            if (!context.Platforms.Any(s => s.Name == searchResults.Results[i].Platforms[j].Name))
                            {
                                Platform newPlatform = new Platform
                                {
                                    Name = searchResults.Results[i].Platforms[j].Name,
                                };
                                context.Platforms.Add(newPlatform);
                                context.SaveChanges();

                                tempPlatform = context.Platforms.Single(c => c.Name == searchResults.Results[i].Platforms[j].Name);
                            }
                            else
                            {
                                tempPlatform = context.Platforms.Single(c => c.Name == searchResults.Results[i].Platforms[j].Name);
                            };
                        }
                    }
                    //end of platform part

                    Game newDBGame = new Game
                    {
                        Name = searchResults.Results[i].Name,
                        //Original_release_date = DateTime.Parse(searchResults.Results[i].Original_release_date),
                        //this should always recieve a date now
                        Original_release_date = DateTime.Parse(date),
                        Platform = tempPlatform,
                        //These should probably be in addgametoday
                        FirstAdded = currentWorkingDate,
                        MostRecentlyAdded = currentWorkingDate,
                        User = GetCurrentUserAsync().Result

                    };

                    Type type = typeof(Image);
                    PropertyInfo[] properties = type.GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        var gv = searchResults.Results[i].Image;
                        var altv = searchResults.Results[i].Image.GetType().GetProperty("Icon_url").GetValue(gv, null);
                        var tryname = property.Name;
                        var svalue2 = searchResults.Results[i].Image.GetType().GetProperty(tryname).GetValue(gv, null);
                        newDBGame.GameImages.GetType().GetProperty(property.Name).SetValue(newDBGame.GameImages, svalue2);
                    }

                    context.Games.Add(newDBGame);
                    context.SaveChanges();
                    //add the game to a day
                    AddGameToDay(context.Games.Single(c => c.Name == searchResults.Results[i].Name), currentWorkingDate);

                }
            }

            IList<Game> games = context.Games.Where(u => u.User == GetCurrentUserAsync().Result).Include(i => i.GameImages).Include(p => p.Platform).OrderByDescending(x => x.MostRecentlyAdded).ToList();

            String thisMonth = currentWorkingDate.ToString("MM-yyyy");
            return RedirectToAction("Monthly", "Chart", new { id = thisMonth });
        }

        public IActionResult AddGame()
        {
            //the get version of this for testing
            //remove this, not bothering to add user info
            IList<Game> games = context.Games.Include(i => i.GameImages).Include(p => p.Platform).OrderByDescending(x => x.MostRecentlyAdded).ToList();
            return View(games);
        }

        [HttpPost]
        public IActionResult AddRecentGame(int gameid)
        {
            Game recentGame = context.Games.Single(c => c.ID == gameid);
            AddGameToDay(recentGame, currentWorkingDate);

            IList<Game> games = context.Games.Where(u => u.User == GetCurrentUserAsync().Result).Include(i => i.GameImages).Include(p => p.Platform).OrderByDescending(x => x.MostRecentlyAdded).ToList();
            String thisMonth = currentWorkingDate.ToString("MM-yyyy");
            return RedirectToAction("Monthly", "Chart", new { id = thisMonth });
        }

        public IActionResult AllGames()
        {
            IList<Game> games = context.Games.Where(u => u.User == GetCurrentUserAsync().Result).Include(c => c.Platform).Include(i => i.GameImages).OrderByDescending(x => x.MostRecentlyAdded).ToList();
            return View(games);
        }

        public IActionResult Days()
        {
            List<Day> days = context.Days.Where(u => u.User == GetCurrentUserAsync().Result).Include(g => g.GamesPlayed).ToList();
            return View(days);
        }

        //can't pass DateTime.Today, nullable DateTime would need to be converted 
        public void AddGameToDay(Game game, DateTime day = default(DateTime))
        {
            //adding to a day adds to days played, this makes some assumptions
            game.DaysPlayed++;

            if (day == default(DateTime))
            {
                day = DateTime.Today;
            }
            if (!context.Days.Any(d => d.CalendarDate == day && d.User == GetCurrentUserAsync().Result))
            {
                Day newDay = new Day();

                newDay.CalendarDate = day;
                newDay.GamesPlayed.Add(game);
                newDay.User = game.User;


                context.Days.Add(newDay);
            }
            else
            {
                Day oldDay = context.Days.Single(d => d.CalendarDate == day && d.User == GetCurrentUserAsync().Result);
                oldDay.GamesPlayed.Add(game);
                //under the assumption that this will eventually take dates other than today
            }

            if (game.MostRecentlyAdded < day)
            {
                game.MostRecentlyAdded = day;
            }

            context.SaveChanges();
            return;
        }

        

        private async Task<String> SendSearchRequest(string searchstring, int page)
            //this assumes page will always be sent
        {
            string url = $@"http://www.giantbomb.com/api/search/?api_key={privateapikey}&format=json&page={page}&query='{searchstring}'&resources=game";
            //per documentation, use one static httpclient per app
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Game tracking demo thing");
            //giantbomb requires a user-agent
            var response = await HttpClient.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            //ViewBag.Urltest = url;

            return result;
        }



        //the file locations here will be an issue
        static string LoadAPI()
        {
            using (StreamReader r = System.IO.File.OpenText(apiFileLocation))
            {
                string api = r.ReadToEnd();
                return api;
            }
        }

        //loads test json files to parse
        public RootObject LoadJson()
        {
            using (StreamReader r = System.IO.File.OpenText("/Data/eternal.json"))
            {
                string json = r.ReadToEnd();
                RootObject items = JsonConvert.DeserializeObject<RootObject>(json);
                return items;
            }
        }

        //this is a debugging thing, remove
        public void TestJson()
        {
            RootObject test = LoadJson();
            return;

        }
    } 
}
