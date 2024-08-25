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

namespace Soul_and_Basil_Project
{
    public partial class FrmPrayTime : Form
    {
        private Timer prayerTimer;
        private DateTime[] prayerTimes;
        private DateTime nextPrayerTime;
        private NotifyIcon AzanNotification;
        private NotifyIcon TasbeehNotification;

        public FrmPrayTime()
        {
            InitializeComponent();
            InitializeNotifications();
            InitializePrayerTimes();
            StartTimer();
        }

        private void InitializeNotifications()
        {
            AzanNotification = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Application
            };

            TasbeehNotification = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Application
            };
        }

        private void InitializePrayerTimes()
        {
            prayerTimes = FetchPrayerTimes();
            if (prayerTimes != null && prayerTimes.Length > 0)
            {
                SetNextPrayerTime();
            }
            else
            {
                MessageBox.Show("Unable to fetch prayer times.");
            }
        }

        private DateTime[] FetchPrayerTimes()
        {
            string url = "https://api.aladhan.com/v1/calendarByCity?city=Mansoura&country=Egypt&method=2";

            using (var client = new WebClient())
            {
                string json = client.DownloadString(url);
                JObject jsonResponse = JObject.Parse(json);
                JArray data = (JArray)jsonResponse["data"];

                DateTime now = DateTime.Now;
                int todayIndex = now.Day - 1; // Adjust for zero-indexing
                if (todayIndex < 0 || todayIndex >= data.Count)
                {
                    return null;
                }

                JObject todayData = (JObject)data[todayIndex];
                JObject timings = (JObject)todayData["timings"];

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
                    MessageBox.Show("Error parsing prayer times: " + ex.Message);
                    return null;
                }

                return prayerTimes;
            }
        }

        private DateTime ParsePrayerTime(string timeString, DateTime referenceDate)
        {
            return DateTime.ParseExact(timeString, "HH:mm", System.Globalization.CultureInfo.InvariantCulture)
                .AddDays(referenceDate.Day - 1)
                .AddMonths(referenceDate.Month - 1)
                .AddYears(referenceDate.Year - 1);
        }

        private void StartTimer()
        {
            prayerTimer = new Timer
            {
                Interval = 1000 // Check every second
            };
            prayerTimer.Tick += PrayTime_Tick;
            prayerTimer.Start();
        }

        private void SetNextPrayerTime()
        {
            DateTime now = DateTime.Now;
            nextPrayerTime = default(DateTime);

            foreach (var time in prayerTimes)
            {
                if (now < time)
                {
                    nextPrayerTime = time;
                    break;
                }
            }

            if (nextPrayerTime == default(DateTime))
            {
                nextPrayerTime = prayerTimes[0].AddDays(1);
            }
        }

        private void PrayTime_Tick(object sender, EventArgs e)
        {
            TimeSpan timeRemaining = nextPrayerTime - DateTime.Now;

            if (timeRemaining.TotalSeconds <= 0)
            {
                // Notify the user
                SendPrayerNotification();

                // Update the next prayer time
                SetNextPrayerTime();
            }
        }

        private void SendPrayerNotification()
        {
            string prayerName = GetPrayerName();
            AzanNotification.BalloonTipIcon = ToolTipIcon.Info;
            AzanNotification.BalloonTipTitle = "Prayer Time";
            AzanNotification.BalloonTipText = $"It's time for {prayerName}.";
            AzanNotification.ShowBalloonTip(1000);
        }

        private string GetPrayerName()
        {
            // Determine the prayer name based on the current time
            DateTime now = DateTime.Now;

            for (int i = 0; i < prayerTimes.Length; i++)
            {
                if (prayerTimes[i] == nextPrayerTime)
                {
                    switch (i)
                    {
                        case 0: return "Fajr";
                        case 1: return "Dhuhr";
                        case 2: return "Asr";
                        case 3: return "Maghrib";
                        case 4: return "Isha";
                    }
                }
            }
            return "Unknown Prayer";
        }
    }
}
