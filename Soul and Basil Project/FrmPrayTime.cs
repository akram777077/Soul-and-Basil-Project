using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Net;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Web.UI.Design.WebControls;
using System.Web.Security;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Soul_and_Basil_Project
{
    public partial class FrmPrayTime : Form
    {
        DateTime[] times;
        TextBox[] boxes;
        TextBox[] namesPrayers;
        byte nextPrayer;
        public FrmPrayTime()
        {
            InitializeComponent();

        }
        private void FrmPrayTime_Load(object sender, EventArgs e)
        {
            boxes = new TextBox[5] { txtfajr, txtzuhr, txtasr, txtmaghrib, txtisha };
            namesPrayers = new TextBox[5] { txtBfajr, txtBzohr, txtBasr, txtBmagrib, txtBisha };
            times = GetPrayerTimes(30.234, 23.32);
            addPrayerTimesToBoxes();
            
        }
        private byte getNextPrayer()
        {
            for(byte i = 0;i<5;i++)
            {
                if (times[i] > DateTime.Now)
                    return i;

            }
            return 0;
        }
        private void PrayTime_Tick(object sender, EventArgs e)
        {
            txtTimeNow.Text = DateTime.Now.ToString("HH:mm:ss");

            txtPrayTimer.Text= (times[3]-DateTime.Now).ToString("hh':'mm':'ss");
            nextPrayer = getNextPrayer();
            textNextPrayer.Text = namesPrayers[nextPrayer].Text;
            textCurrentPrayer.Text = namesPrayers[(nextPrayer==0)?4:nextPrayer - 1].Text;
            txtHijri.Text=GetCompleteDateInArabic();
            txtTodayMiladi.Text = GetCompleteGregorianDateWithArabicDaysAndMonths();
        }

        private const string ApiUrlTemplate = "https://api.aladhan.com/v1/timings/{0}?latitude={1}&longitude={2}&method=2";

        public static DateTime[] GetPrayerTimes(double latitude, double longitude)
        {
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string url = string.Format(ApiUrlTemplate, date, latitude, longitude);

            using (var client = new WebClient())
            {
                string json = client.DownloadString(url);
                JObject jsonResponse = JObject.Parse(json);

                JObject timings = (JObject)jsonResponse["data"]["timings"];

                DateTime now = DateTime.Now;
                DateTime[] prayerTimes = new DateTime[5];

                try
                {
                    prayerTimes[0] = ParsePrayerTime(timings["Fajr"].ToString(), now);
                    prayerTimes[1] = ParsePrayerTime(timings["Dhuhr"].ToString(), now);
                    prayerTimes[2] = ParsePrayerTime(timings["Asr"].ToString(), now);
                    prayerTimes[3] = ParsePrayerTime(timings["Maghrib"].ToString(), now);
                    prayerTimes[4] = ParsePrayerTime(timings["Isha"].ToString(), now);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing prayer times: " + ex.Message);
                    return null;
                }

                return prayerTimes;
            }
        }

        private static DateTime ParsePrayerTime(string timeString, DateTime referenceDate)
        {
            return DateTime.ParseExact(timeString, "HH:mm", System.Globalization.CultureInfo.InvariantCulture)
                .AddSeconds(-referenceDate.Second)
                .AddMilliseconds(-referenceDate.Millisecond)
                .AddTicks(-(referenceDate.Ticks % TimeSpan.TicksPerSecond));
        }

        private void addPrayerTimesToBoxes()
        {
            

            for (short i = 0; i < 5; i++) 
            {
                boxes[i].Text = times[i].ToString("HH:mm");
            }
        }

        private void txtisha_TextChanged(object sender, EventArgs e)
        {

        }
        public string GetCompleteDateInArabic()
        {
            CultureInfo arabicCulture = new CultureInfo("ar-SA");
            DateTime today = DateTime.Now;
            string completeDateInArabic = today.ToString("dddd, dd MMMM yyyy", arabicCulture);
            return completeDateInArabic;
        }
        private static readonly string[] ArabicMonths = new string[]
        {
            "يناير",
            "فبراير",
            "مارس",
            "أبريل",
            "مايو",
            "يونيو",
            "يوليو",
            "أغسطس",
            "سبتمبر",
            "أكتوبر",
            "نوفمبر",
            "ديسمبر"
        };

        private static readonly string[] ArabicDays = new string[]
        {
            "الأحد",
            "الإثنين",
            "الثلاثاء",
            "الأربعاء",
            "الخميس",
            "الجمعة",
            "السبت"
        };

        public string GetCompleteGregorianDateWithArabicDaysAndMonths()
        {
            DateTime today = DateTime.Now;
            string dayOfWeek = ArabicDays[(int)today.DayOfWeek];
            string dayOfMonth = today.Day.ToString("00");
            string monthInArabic = ArabicMonths[today.Month - 1];
            string year = today.Year.ToString();
            return $"{dayOfWeek}, {dayOfMonth} {monthInArabic} {year}";
        }
    }
}
