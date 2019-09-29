using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tweetinvi;

namespace Examplinvi.WinFormsApp
{
    static class Program
    {
        static Program()
        {
            SetCreds();
        }
        static bool credsAreSet = false;
        static void SetCreds()
        {
            if (credsAreSet) return;
            credsAreSet = true;
          
            Auth.SetUserCredentials(Creds.Settings.CONSUMER_KEY, Creds.Settings.CONSUMER_SECRET,
                Creds.Settings.ACCESS_TOKEN, Creds.Settings.ACCESS_TOKEN_SECRET);
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
           

        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
