using Examplinvi.DbFx;
using Examplinvi.DbFx.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tweetinvi;
using Tweetinvi.Logic.DTO;
using Tweetinvi.Models;

namespace Examplinvi.WinFormsApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var dir = @"c:\users\alexander.higgins\pictures\exposecnn";
            var helper = new Helper();
            for (var i = 1; i <= 12; i++)
            {
                var fileName = $"Expose CNN - Release 1 Part {i} of 12.mp4";
                var filename = $"Expose CNN - Release 1 - Part {i} of 12.mp4";
                var mediaPath = System.IO.Path.Combine(dir, fileName);

                string text = $@"#ExposeCNN - Release 1 - PART {i} of 12: CNN Insider Blows Whistle on Network President Jeff Zucker’s Personal Vendetta Against POTUS

#ExposeCNNDay";
                if (!File.Exists(mediaPath))
                {
                    string bp = "";
                }
                Console.WriteLine($"uploading part {i}");
                var video = Helper.UploadVideo(mediaPath);
                var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                {
                    Medias = new List<IMedia>() { video }

                };
                Console.WriteLine($"publishing part {i}");
                Tweet.PublishTweet(text, tParams);
            }


            this.FormClosing += Form1_FormClosing;
        }

        private bool isClosing;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.isClosing = true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var urlInput = new UrlInput())
            {
                if (urlInput.ShowDialog() == DialogResult.OK && urlInput.URIs.Count > 0)
                {
                    urlInput.URIs.ForEach(uri => Helper.DownloadVideo(uri));
                }
            }
        }

        private void autofollowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.AutoFollowing)
            {
                this.isClosing = true;
                while (this.AutoFollowing)
                {
                    System.Threading.Thread.Sleep(0100);
                }
            }
            using (var urlInput = new UrlInput())
            {
                if (urlInput.ShowDialog() == DialogResult.OK && urlInput.URIs.Count == 1)
                {
                    var tUrl = urlInput.URIs.First();
                    var t = Helper.GetTweet(tUrl);
                    AutoFollowRts(t);
                    //this.autofollowToolStripMenuItem.Enabled = false;
                }
            }
        }

        private bool AutoFollowing;
        private void AutoFollowRts(ITweet t)
        {
            if (!AutoFollowing)
                Task.Run(() => AutoFollowRtsAsync(t));

        }

        ConcurrentQueue<Func<long>> afQueue = null;

        private void AutoFollowRtsAsync(ITweet t)
        {
            AutoFollowing = true;
            afQueue = new ConcurrentQueue<Func<long>>();
            Task.Run(() => ProcessAfQueue());
            while (!isClosing)
            {


                var ids = Tweet.GetRetweetersIds(t.Id).ToList();
                using (var ctx = new DbFx.TDbContext())
                {
                    var followingIds = (from u in ctx.Users where ids.Contains(u.Id) select u.Id).ToList();

                    var autoUnfollowIds = (from u in ctx.Users where ids.Contains(u.Id) select u.Id).ToList();
                    var filtered = ids.Except(followingIds).Except(autoUnfollowIds).ToList();
                    filtered.ForEach(x => afQueue.Enqueue(() => x));

                    System.Threading.Thread.Sleep(60 * 10000);
                }


                //var actions = filtered.Select(x => (Func<bool>)(() => User.FollowUser(x))).ToList();
                //Actions.Foreach()

            }
            this.AutoFollowing = false;
        }

        private void ProcessAfQueue()
        {
            var myUser = User.GetAuthenticatedUser();
            while (!isClosing)
            {
                while (afQueue.Count > 0)
                {
                    if (afQueue.TryDequeue(out Func<long> del))
                    {
                        var userId = del();
                        if (userId == myUser.Id) continue;

                        var user = User.GetUserFromId(userId);
                

                        DbUser dbUser = null;
                        using (var ctx = new TDbContext())
                        {

                            dbUser = ctx.Users.FirstOrDefault(x => x.Id == user.Id);
                            if (dbUser == null)
                            {

                                var rel = Friendship.GetRelationshipDetailsBetween(user.Id, myUser.Id);
                                dbUser = user.ToDbUser(rel.Following);
                                ctx.Users.Add(dbUser);
                               
                            }
                            dbUser.Following = true;
                            ctx.SaveChanges();
                            User.FollowUser(user.Id);
                            System.Diagnostics.Debug.WriteLine($"Auto Followed {dbUser.ScreenName}");
                        }
                        System.Threading.Thread.Sleep(5000);
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
    }

    public class Helper
    {
        static bool credsAreSet = false;
        private static readonly DirectoryInfo MediaDirectory = Directory.CreateDirectory(@"C:\Source\Repos\tweetinvi\Examplinvi.InviConsole\Examplinvi.InviConsole\bin\Debug\media");
        static void SetCreds()
        {
            if (credsAreSet) return;
            credsAreSet = true;
            //Auth.SetUserCredentials("CONSUMER_KEY", "CONSUMER_SECRET", "ACCESS_TOKEN", "ACCESS_TOKEN_SECRET");
            Auth.SetUserCredentials(Creds.Settings.CONSUMER_KEY, Creds.Settings.CONSUMER_SECRET,
                Creds.Settings.ACCESS_TOKEN, Creds.Settings.ACCESS_TOKEN_SECRET);
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
            // Publish the Tweet "Hello World" on your Timeline
            //Tweet.PublishTweet("Hello World!");


        }

        #region Helpers
        static Helper()
        {
            SetCreds();
        }


        public static long GetId(string url) => GetId(new Uri(url));

        public static long GetId(Uri uri)
        {
            var path = uri.AbsolutePath;
            var segments = path.Split('/');
            long id = 0;
            for (var i = 0; !long.TryParse(segments[i], out id) && i < segments.Length; i++)
            {
            }
            return id;
        }
        #endregion

        #region Upload
        public static IMedia UploadImage(string filepath)
        {
            var imageBinary = File.ReadAllBytes(filepath);
            var media = Upload.UploadBinary(imageBinary);
            return media;
        }

        public static IMedia UploadVideo(string filepath)
        {
            var videoBinary = File.ReadAllBytes(filepath);

            var media = Upload.UploadVideo(videoBinary, new Tweetinvi.Core.Public.Parameters.UploadVideoOptionalParameters()
            {
                UploadStateChanged = uploadChangeEventArgs =>
                {
                    Console.WriteLine(uploadChangeEventArgs.Percentage);
                }
            });

            return media;
        }

        #endregion

        #region Tweet
        static void CloneAndPubWithMedia(string url, string text = null)
        {

            var t = Tweet.GetTweet(GetId(url));

            var mediaPath = DownloadMedia(t);
            var video = UploadVideo(mediaPath);

            if (text == null)
            {
                text = $"@{t.CreatedBy.ScreenName}: {t.Text} \r\n\r\n via @{User.GetAuthenticatedUser().ScreenName}";
            }
            var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
            {
                Medias = new List<IMedia>() { video }
            };
            Tweet.PublishTweet(text ?? t.Text, tParams);
        }

        static void TweetWithVideo(string text, string mediaPath)
        {
            SetCreds();
            var video = UploadVideo(mediaPath);
            var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
            {
                Medias = new List<IMedia>() { video }

            };
            Tweet.PublishTweet(text, tParams);
        }

        #endregion

        #region Download

        static void DownloadVideos(params string[] urls) => DownloadVideos(urls.Select(x => new Uri(x)).ToArray());
        static void DownloadVideos(params Uri[] uris) => uris.ToList().ForEach(uri => DownloadVideo(uri));
        public static void DownloadVideo(Uri uri)
        {
            SetCreds();
            var id = GetId(uri);
            var t = Tweet.GetTweet(id);
            DownloadMedia(t);
            //var m = t.Media.First(x => ; x.MediaType == "video");
        }
        static void DownloadVideo(string url) => DownloadVideo(new Uri(url));





        static string DownloadMedia(ITweet t)
        {
            var m = t.Media.First(x => x.MediaType == "video");
            var variants = m.VideoDetails.Variants.Where(x => x.ContentType == "video/mp4");
            var maxBitRate = variants.Max(x => x.Bitrate);
            var variant = variants.First(x => x.Bitrate == maxBitRate);
            var fileName = Path.GetFileName(variant.URL.Split('?')[0]);
            var mediaDirectory = Directory.CreateDirectory("Media");
            var dest = Path.Combine(mediaDirectory.FullName, fileName);
            if (File.Exists(dest))
            {
                Console.WriteLine("File exists to {0}", dest);
                return dest;
            }
            var id = t.Id;

            var data = TwitterAccessor.DownloadBinary(variant.URL);
            //var fileName = $"{id}.{m.VideoDetails.Variants.First(x=> x.ContentType== "video/mp4").ContentType.Split('/')[1]}";


            File.WriteAllBytes(dest, data);
            Console.WriteLine("Saved to {0}", dest);
            return dest;
        }

        internal static ITweet GetTweet(Uri tUrl)
        {
            var id = GetId(tUrl);
            return Tweet.GetTweet(id);
        }

        #endregion
    }
}
