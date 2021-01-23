using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

namespace Examplinvi.MyConsole
{
    class Program
    {
        static Program()
        {
            Creds.Helper.SetCreds();
        }
        static void Main(string[] args)
        {
            MonitorStream();
        }


        private static void MonitorStream()
        {
            var stream = Stream.CreateFilteredStream();
            var t = stream.GetType().Name;
            var current = User.GetAuthenticatedUser();
            stream.AddFollow(current);
            stream.MatchingTweetReceived += Stream_MatchingTweetReceived;
            stream.NonMatchingTweetReceived += Stream_NonMatchingTweetReceived;
            stream.JsonObjectReceived += Stream_JsonObjectReceived;
            stream.UnmanagedEventReceived += Stream_UnmanagedEventReceived;
            stream.StreamStopped += (sender, e) => { stream.StartStreamMatchingAnyCondition(); };
           
            stream.StartStreamMatchingAnyCondition();
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
                if (bool.Parse(bool.TrueString))
                {
                    System.Console.WriteLine("Hello");
                }
            }


        }

        private static void Stream_UnmanagedEventReceived(object sender, Tweetinvi.Events.UnmanagedMessageReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Stream_JsonObjectReceived(object sender, Tweetinvi.Events.JsonObjectEventArgs e)
        {
            string bp = e.Json;
           // throw new NotImplementedException();
        }

        private static void Stream_NonMatchingTweetReceived(object sender, Tweetinvi.Events.TweetEventArgs e)
        {
            Process(e.Tweet);
        }

        private static void Stream_MatchingTweetReceived(object sender, Tweetinvi.Events.MatchedTweetReceivedEventArgs e)
        {
            Process(e.Tweet);
        }
        private static void Process(ITweet tweet)
        {
            if (tweet.IsRetweet)
            {
                Console.WriteLine($"{DateTime.Now}: @{tweet.CreatedBy.ScreenName} - {tweet.Text}");
            }
            else
            {
                LogDebug($"{DateTime.Now}: " + tweet.ToJson() + "\r\n\r\n");
            }
        }
        private static void LogDebug(string format, params object[] args) => LogDebug(string.Format(format, args));
        private static void LogDebug(string message)
        {
            System.Console.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}
