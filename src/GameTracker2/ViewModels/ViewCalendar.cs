﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;


namespace GameTracker2.ViewModels
{
    public class ViewCalendar
    {

        public static List<int> GenCalendar(DateTime sentDate = default(DateTime))
        {
            if (sentDate == default(DateTime))
                sentDate = DateTime.Today;

            Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            int daysinmonth = myCal.GetDaysInMonth(myCal.GetYear(sentDate), myCal.GetMonth(sentDate));
            DateTime startOfMonth = new DateTime(myCal.GetYear(sentDate), myCal.GetMonth(sentDate), 1);

            List<int> returnList = new List<int>();

            DateTime topWeek = startOfMonth;
            int count = 0;
            while (topWeek.ToString("ddd") != "Sun")
            {
                topWeek = topWeek.AddDays(-1);
                count++;
            }

            for (int i = 0; i < (daysinmonth + count); i++)
            {
                if (i < count)
                {
                    returnList.Add(0);
                    topWeek = topWeek.AddDays(1);
                }
                else
                {
                    returnList.Add(topWeek.Day);
                    topWeek = topWeek.AddDays(1);
                }
            }

            return returnList;
        }


    }
}
