using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GameTracker2.Controllers
{
    public class ErrorController : Controller
    {
        public ViewResult Error() => View();
    }
}