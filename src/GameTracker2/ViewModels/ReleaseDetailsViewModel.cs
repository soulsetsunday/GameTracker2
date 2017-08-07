using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameTracker2.ViewModels
{
    public class ReleaseDetailsViewModel
    {
        public int GameID { get; set; }
        public int PlatformID { get; set; }
        public string ViewDate { get; set; }
        public List<string> ReleaseDates { get; set; }
        public List<ViewDateDetail> ViewDateDetails { get; set; } 
    }

    public class ViewDateDetail
    {
        public string ReleaseDetailDate { get; set; }
        public string ReleaseDetailRegion { get; set; }
        public string ReleaseDetailPlatoform { get; set; }
    }
}
