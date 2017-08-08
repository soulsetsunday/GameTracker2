﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using GameTracker2.Classes;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace GameTracker2.Controllers
{
    public class CalendarController : Controller
    {
        //This uses a datetime string to show past months, this month with no string
        public IActionResult Index(string id = null)
        {

            DateTime sentDate = new DateTime();

            if (id == null)
                sentDate = DateTime.Today;
            else
                if (DateTime.TryParseExact(id, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out sentDate))
                //setting this to the first would maybe look better, seems to serve no purpose
                return View(sentDate);
                else
                sentDate = DateTime.Today;

            return View(sentDate);
        }

        public IActionResult EditAll(string id = null)
        {
            DateTime sentDate = new DateTime();

            if (DateTime.TryParseExact(id, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out sentDate))
                //setting this to the first would maybe look better, seems to serve no purpose
                return View(sentDate);
            else
                sentDate = DateTime.Today;

            return View(sentDate);
        }
    }
}
