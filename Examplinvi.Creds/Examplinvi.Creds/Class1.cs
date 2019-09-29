using System;
using Tweetinvi;

namespace Examplinvi.Creds
{
    public class Helper
    {
        static bool credsAreSet = false;
        public static void SetCreds()
        {
            if (credsAreSet) return;
            credsAreSet = true;

            Auth.SetUserCredentials(Creds.Settings.CONSUMER_KEY, Creds.Settings.CONSUMER_SECRET,
                Creds.Settings.ACCESS_TOKEN, Creds.Settings.ACCESS_TOKEN_SECRET);
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;

        }
    }
}
