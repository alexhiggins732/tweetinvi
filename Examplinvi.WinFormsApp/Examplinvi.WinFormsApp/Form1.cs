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
            //            var dir = @"c:\users\alexander.higgins\pictures\exposecnn";
            //            var helper = new Helper();
            //            for (var i = 1; i <= 12; i++)
            //            {
            //                var fileName = $"Expose CNN - Release 1 Part {i} of 12.mp4";
            //                var filename = $"Expose CNN - Release 1 - Part {i} of 12.mp4";
            //                var mediaPath = System.IO.Path.Combine(dir, fileName);

            //                string text = $@"#ExposeCNN - Release 1 - PART {i} of 12: CNN Insider Blows Whistle on Network President Jeff Zucker’s Personal Vendetta Against POTUS

            //#ExposeCNNDay";
            //                if (!File.Exists(mediaPath))
            //                {
            //                    string bp = "";
            //                }
            //                Console.WriteLine($"uploading part {i}");
            //                var video = Helper.UploadVideo(mediaPath);
            //                var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
            //                {
            //                    Medias = new List<IMedia>() { video }

            //                };
            //                Console.WriteLine($"publishing part {i}");
            //                Tweet.PublishTweet(text, tParams);
            //            }


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

        private void retweetWithVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var urlInput = new UrlInput())
            {
                urlInput.Text = "Enter Text";
                if (urlInput.ShowDialog() == DialogResult.OK)
                {
                    var text = urlInput.TextBox.Text;
                    var idx = text.LastIndexOf("http");
                    var start = text.Substring(idx);
                    var endIdx = start.IndexOfAny(new[] { ' ', '\r', '\n' });
                    var url = start;
                    if (endIdx > -1)
                        url = start.Substring(0, endIdx);
                    var uri = new Uri(url);
                    var mediaPath = Helper.DownloadVideo(uri);

                    var tweetText = text.Replace(url, "");
                    var media = Helper.UploadVideo(mediaPath);
                    var tParamers = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                    {
                        Medias = new List<IMedia>() { media }
                    };
                    Console.WriteLine($"published {media.MediaId}");
                    var t = Tweet.PublishTweet(tweetText + " ", tParamers);
                    if (t == null)
                    {
                        var videoUrl = Helper.GetVideoUrl(uri);
                        t = Tweet.PublishTweet(tweetText + " " + videoUrl);
                    }
                    Console.WriteLine($"{t.Id}");

                }
            }
        }

        private void retweetWithMediaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var urlInput = new UrlInput())
            {
                urlInput.Text = "Enter Text";
                if (urlInput.ShowDialog() == DialogResult.OK)
                {
                    var text = urlInput.TextBox.Text;
                    var idx = text.LastIndexOf("http");
                    var start = text.Substring(idx);
                    var endIdx = start.IndexOfAny(new[] { ' ', '\r', '\n' });
                    var url = start;
                    if (endIdx > -1)
                        url = start.Substring(0, endIdx);
                    var uri = new Uri(url);
                    var mediaPath = Helper.DownloadMedia(uri);

                    var tweetText = text.Replace(url, "").Trim();
                    var len = tweetText.Length;
                    var media = Helper.UploadImage(mediaPath);
                    var tParamers = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                    {
                        Medias = new List<IMedia>() { media }
                    };
                    var t = Tweet.PublishTweet(tweetText, tParamers);
                    Console.WriteLine($"Published {t.Id}");
                }
            }
        }

        private void LoadTweet()
        {
            var urlText = this.txtinput.Text;
            if (Uri.TryCreate(urlText, UriKind.Absolute, out Uri result))
            {
                var tweetId = Helper.GetId(result);
                var tweet = Tweet.GetTweet(tweetId);
                this.txtRawText.Text = tweet.FullText;

            }
            else
            {
                MessageBox.Show("Invalid uri");
            }

        }
        private void loadTweetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var urlInput = new UrlInput())
            {
                urlInput.Text = "Enter Url";
                if (urlInput.ShowDialog() == DialogResult.OK && urlInput.URIs.Count == 1)
                {
                    var tweetId = Helper.GetId(urlInput.URIs.First());
                    var tweet = Tweet.GetTweet(tweetId);
                    using (var urlInput2 = new UrlInput())
                    {
                        urlInput2.TextBox.Text = tweet.FullText;
                        urlInput2.Text = "Loaded Text";
                        if (urlInput2.ShowDialog() == DialogResult.OK)
                        {
                            Tweet.PublishTweet(urlInput2.TextBox.Text);
                        }
                    }
                }
                //https://twitter.com/MailOnline/status/1231291084669685760
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadTweet();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                Send();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error", ex.Message);
            }
        }
        private void Send()
        {
            this.toolStripStatusLabel1.Text = $"[{DateTime.Now}] Publishing...";
            if (this.rbVideo.Checked || rbImage.Checked || rbDownload.Checked)
            {
                var text = txtRawText.Text;
                var idx = text.LastIndexOf("http");
                var start = text.Substring(idx);
                var endIdx = start.IndexOfAny(new[] { ' ', '\r', '\n' });
                var url = start;
                if (endIdx > -1) url = start.Substring(0, endIdx);
                var uri = new Uri(url);
                IMedia media = null;
                var tweetText = text.Replace(url, "");
                if (rbVideo.Checked || rbDownload.Checked)
                {
                    var mediaPath = Helper.DownloadVideo(uri);
                    if (rbDownload.Checked)
                    {
                        this.toolStripStatusLabel1.Text = $"[{DateTime.Now}] Downloaded {Path.GetFileName(mediaPath)}";
                        return;
                    }
                    media = Helper.UploadVideo(mediaPath);

                }
                else
                {
                    var mediaPath = Helper.DownloadMedia(uri);
                    media = Helper.UploadImage(mediaPath);
                }
                var tParamers = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                {
                    Medias = new List<IMedia>() { media }
                };

                var t = Tweet.PublishTweet(tweetText, tParamers);

                if (t == null)
                {
                    var mediaUrl = Helper.GetVideoUrl(uri);
                    t = Tweet.PublishTweet(tweetText + " " + url);
                }


                this.toolStripStatusLabel1.Text = $"[{DateTime.Now}] Published: {t.Id}";
                Console.WriteLine($"{t.Id}");
            }
            else
            {
                ITweet t;
                if (rbUpdate.Checked)
                {
                    var json = File.ReadAllText("updatemedia.json");
                    var media = JsonSerializer.ConvertJsonTo<Tweetinvi.Logic.Model.Media>(json);
                    var tParamers = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                    {
                        Medias = new List<IMedia>() { media }
                    };
                    t = Tweet.PublishTweet(txtRawText.Text, tParamers);
                }
                else
                {
                    t = Tweet.PublishTweet(txtRawText.Text);
                }

                this.toolStripStatusLabel1.Text = $"[{DateTime.Now}] Published: {t.Id}";
                //this.toolStripStatusLabel1.Text = $"{t.Id}";
                Console.WriteLine($"{t.Id}");
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

            var mediaPath = DownloadVido(t);
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
        public static string DownloadVideo(Uri uri)
        {
            SetCreds();
            if (uri.Host == "t.co")
            {
                var url = uri.ToString();
                var req = System.Net.WebRequest.CreateHttp(url);
                var res = req.GetResponse();
                var resUri = res.ResponseUri;
                var resUrl = resUri.ToString();
                var statusUri = resUrl.Substring(0, resUrl.IndexOf("/video"));
                uri = new Uri(statusUri);
                //var fileName = Path.GetFileName(url.Split('/').Last());
                //var mediaDirectory = Directory.CreateDirectory("Media");
                //var dest = Path.Combine(mediaDirectory.FullName, fileName + ".mp4");
                //if (File.Exists(dest))
                //{
                //    Console.WriteLine("File exists to {0}", dest);
                //    return dest;
                //}
                
                //var data = TwitterAccessor.DownloadBinary(resUri.ToString());
                //File.WriteAllBytes(dest, data);
                //Console.WriteLine("Saved to {0}", dest);

            }
            var id = GetId(uri);
            var t = Tweet.GetTweet(id);
            return DownloadVido(t);
            //var m = t.Media.First(x => ; x.MediaType == "video");
        }
        static string DownloadVideo(string url) => DownloadVideo(new Uri(url));
        public static string DownloadMedia(Uri uri)
        {
            SetCreds();
            var id = GetId(uri);
            var t = Tweet.GetTweet(id);
            return DownloadMedia(t);
            //var m = t.Media.First(x => ; x.MediaType == "video");
        }
        static string DownloadMedia(string url) => DownloadMedia(new Uri(url));


        static string DownloadMedia(ITweet t)
        {
            var m = t.Media.First(); // (x => x.MediaType == "video");
                                     //var variants = m.VideoDetails.Variants.Where(x => x.ContentType == "video/mp4");

            //var maxBitRate = variants.Max(x => x.Bitrate);
            //var variant = variants.First(x => x.Bitrate == maxBitRate);
            var fileName = Path.GetFileName(m.MediaURL.Split('?')[0]);
            var mediaDirectory = Directory.CreateDirectory("Media");
            var dest = Path.Combine(mediaDirectory.FullName, fileName);
            if (File.Exists(dest))
            {
                Console.WriteLine("File exists to {0}", dest);
                return dest;
            }
            var id = t.Id;

            var data = TwitterAccessor.DownloadBinary(m.MediaURL);
            //var fileName = $"{id}.{m.VideoDetails.Variants.First(x=> x.ContentType== "video/mp4").ContentType.Split('/')[1]}";


            File.WriteAllBytes(dest, data);
            Console.WriteLine("Saved to {0}", dest);
            return dest;
        }


        static string DownloadVido(ITweet t)
        {
            
            var m = t.Media.FirstOrDefault(x => x.MediaType == "video");
            if (m == null)
            {
                m = t.Media.First();
                var expandedURL = m.ExpandedURL;
                var mediaDirectory1 = Directory.CreateDirectory("Media");
                var fileName1 = Path.GetFileNameWithoutExtension(m.MediaURL) + ".mp4" ;
                var dest1 = Path.Combine(mediaDirectory1.FullName, fileName1);
                if (File.Exists(dest1))
                {
                    Console.WriteLine("File exists to {0}", dest1);
                    return dest1;
                }
                var data1 = TwitterAccessor.DownloadBinary(expandedURL);
                File.WriteAllBytes(dest1, data1);
                File.SetLastWriteTime(dest1, DateTime.Now);
                Console.WriteLine("Saved to {0}", dest1);
                return dest1;
            }
            //

            var variants = m.VideoDetails.Variants.Where(x => x.ContentType == "video/mp4");
            var mediaDirectory = Directory.CreateDirectory("Media");
            //foreach (var video in variants)
            //{
            //    var videoFile = Path.GetFileName(video.URL.Split('?')[0]);
            //    var name = Path.GetFileNameWithoutExtension(videoFile);
            //    var ext = Path.GetExtension(videoFile);
            //    var videoFileName = $"{name}-{video.Bitrate}.ext";
            //    var videoData= TwitterAccessor.DownloadBinary(video.URL);
            //    var videoDest = Path.Combine(mediaDirectory.FullName, videoFileName);

            //    File.WriteAllBytes(videoDest, videoData);
            //}
            var maxBitRate = variants.Max(x => x.Bitrate);
            var variant = variants.First(x => x.Bitrate == maxBitRate);
            var fileName = Path.GetFileName(variant.URL.Split('?')[0]);

            var dest = Path.Combine(mediaDirectory.FullName, fileName);
            if (File.Exists(dest))
            {
                Console.WriteLine("File exists to {0}", dest);
                return dest;
            }
            var id = t.Id;
            //var hc = "https://video.twimg.com/ext_tw_video/1231476970950541312/pu/vid/360x640/LQfjTBt4K6vM51Fy.mp4?tag=10";
            //var fileName1 = Path.GetFileName(hc.Split('?')[0]);
            //var data1 = TwitterAccessor.DownloadBinary(hc);
            //File.WriteAllBytes(Path.Combine(mediaDirectory.FullName, fileName1), data1);
            var data = TwitterAccessor.DownloadBinary(variant.URL);
            //var data = TwitterAccessor.DownloadBinary(variant.URL);
            //var fileName = $"{id}.{m.VideoDetails.Variants.First(x=> x.ContentType== "video/mp4").ContentType.Split('/')[1]}";


            File.WriteAllBytes(dest, data);
            File.SetLastWriteTime(dest, DateTime.Now);
            Console.WriteLine("Saved to {0}", dest);
            return dest;
        }

        internal static ITweet GetTweet(Uri tUrl)
        {
            var id = GetId(tUrl);
            return Tweet.GetTweet(id);
        }

        internal static string GetVideoUrl(Uri uri)
        {
            var tweet = GetTweet(uri);
            var m = tweet.Media.First(x => x.MediaType == "video");
            return m.URL;
        }

        #endregion
    }
}
