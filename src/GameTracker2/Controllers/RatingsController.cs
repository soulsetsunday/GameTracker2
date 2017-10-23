using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GameTracker2.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using GameTracker2.Classes;
using Microsoft.AspNetCore.Identity;

namespace GameTracker2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class RatingsController : Controller
    {
        private GameDbContext context;
        private readonly UserManager<ApplicationUser> _userManager;
        public RatingsController(GameDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            context = dbContext;
            _userManager = userManager;
        }
        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        public IActionResult Index()
        {
            IList<Game> games = context.Games.Where(u => u.User == GetCurrentUserAsync().Result).Include(c => c.Platform).Include(i => i.GameImages).OrderByDescending(x => x.Review).ToList();
            return View(games);
        }
        [HttpPost]
        public IActionResult Index(int gameid, int review)
        {
            Game reviewgame = context.Games.Single(g => g.User == GetCurrentUserAsync().Result && g.ID == gameid);
            reviewgame.Review = review;
            context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Goty()
        {
            IList<Game> games = context.Games.Where(g => g.User == GetCurrentUserAsync().Result && g.Original_release_date.Year == DateTime.Today.Year).Include(c => c.Platform).Include(i => i.GameImages).OrderByDescending(x => x.Review).ToList();
            return View("Index", games);
        }
    }
}