using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;

using System.Linq;
using System.Net;
using System.Text;
using Tweetinvi;
using Tweetinvi.Models;

namespace Examplinvi.NETFramework
{
    class StatProcessor
    {
        static string updateXpath = "$('#innercontent ul').first().find('li').length;";
        static string statsQuery = ".dataTable  tbody tr";
        static IMedia UpdateMedia;
        static void AssureUpdateMedia()
        {
            if (System.IO.File.Exists("updatemedia.json"))
            {
                var json = System.IO.File.ReadAllText("updatemedia.json");
                UpdateMedia = JsonSerializer.ConvertJsonTo<Tweetinvi.Logic.Model.Media>(json);
            }
            else
            {
                var binary = System.IO.File.ReadAllBytes("KR3AT CORONAVIRUS UPDATES.png");
                var media = Upload.UploadBinary(binary);
                var json = media.ToJson();
                var fromJson = JsonSerializer.ConvertJsonTo<Tweetinvi.Logic.Model.Media>(json);
                System.IO.File.WriteAllText("updatemedia.json", json);
                UpdateMedia = media;
            }


        }
        internal static void Run()
        {
            var txt = "";
            AssureUpdateMedia();
            var data = GetData();
            System.Console.Title = "Getting data";
            //var usData = GetData();
            var coll = CvCollection.Create(data);
            System.Console.Title = $"[{DateTime.Now}] Sleeping 30 seconds";
            if (!System.Diagnostics.Debugger.IsAttached)
                System.Threading.Thread.Sleep(30000);
            bool pubbedAggr = false;
            int lastCTotal = 0;
            int lastDTotal = 0;
            int lastCtRowCount = 0;
            DateTime lastUpdated = DateTime.Now;
            DateTime lastTableDate = DateTime.Now.AddMinutes(-15);

            int sleep = 15 * 60 * 1000;
            while (true)
            {
                restart:
                var start = DateTime.Now;
                System.Console.Title = $"[{DateTime.Now}] Refreshing data";
                data = GetData();
                var temp = data.Clone();

                if (data.Rows.Count == 0)
                {
                    System.Console.Title = $"[{DateTime.Now}] Failed to retrieve data";
                    System.Threading.Thread.Sleep(sleep);
                    continue;

                }
                var newColl = CvCollection.Create(data);
                var dCount = newColl.Stats.Sum(x => x.TotalDCount);
                var cCount = newColl.Stats.Sum(x => x.TotalCount);
                Console.Title = $"[{DateTime.Now.ToLongTimeString()}] {cCount} - {dCount} [{lastUpdated.ToLongTimeString()}] ";
                var dRotwCount = newColl.Stats.Where(x => x.Loc != "China").Sum(x => x.TotalDCount);
                var cRotwCount = newColl.Stats.Where(x => x.Loc != "China").Sum(x => x.TotalCount);

                var newRotwCCount = newColl.Stats.Where(x => x.Loc != "China").Sum(x => x.DailyCount);
                var newRotwDCount = newColl.Stats.Where(x => x.Loc != "China").Sum(x => x.DailyDCount);

                var newCCount = newColl.Stats.Sum(x => x.DailyCount);
                var newDCount = newColl.Stats.Sum(x => x.DailyDCount);


                var changes = newColl.GetChanges(coll);
                if (changes.Count > 0)
                {
                    if (HasBadData(changes))
                    {
                        System.Threading.Thread.Sleep(30000);
                        goto restart;
                    }
                }

                var top = newColl.Stats.OrderByDescending(x => x.TotalCount).Take(30).Select(x => x.Loc);
                var topChanges = changes.Where(x => top.Contains(x.New.Loc));
                foreach (var change in topChanges)
                {

                    var changeText = change.ToString().Replace(", with reported today.", "."); ;
                    if (changeText.Contains("for the first time"))
                    {
                        string bp = "";
                    }
                    var Processor = new TemplateProcessor(changeText);
                    var tagged = Processor.GetProcessedText();
                    Console.WriteLine($"[{ DateTime.Now}] {tagged} ");
                    var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                    {
                        Medias = new List<IMedia>() { UpdateMedia }
                    };
                    Tweet.PublishTweet(tagged, tParams);
                    System.Threading.Thread.Sleep(120000);
                    lastUpdated = DateTime.Now;
                }
                coll = newColl;
                if (!pubbedAggr || DateTime.Now.Subtract(lastTableDate).TotalMinutes >= 15 && changes.Count > 0)
                {
                    if (bool.Parse(bool.FalseString))
                    {
                        data = GetData();
                    }

                    lastTableDate = DateTime.Now;
                    var formatted = $"Update: #CoronaVirus: To date a total of {dCount.ToString("N0")} total deaths and {cCount.ToString("N0")} total #covid19 cases have been confirmed worldwide.\n\nA total of {cRotwCount.ToString("N0")} cases and {dRotwCount.ToString("N0")} deaths have been reported outside of China.\n\n#CoronaVirusOutbreak";
                    //if()
                    if (changes.Any(x => x.New.Loc != "China") || cRotwCount != lastCtRowCount)
                        formatted = $"#CoronaVirus Outside of China - {cRotwCount.ToString("N0")} cases and {dRotwCount.ToString("N0")} deaths.\n\nTo date a total of {dCount.ToString("N0")} deaths and {cCount.ToString("N0")} total #covid19 cases have been confirmed worldwide.\n\n#CoronaVirusOutbreak";

                    data.Columns[0].ColumnName = $"Locations ({data.Rows.Count})";
                    Console.WriteLine($"[{ DateTime.Now}] {formatted} ");

                    var hiddenRow = data.NewRow();
                    hiddenRow[0] = "Other";
                    data.Rows.Add(hiddenRow);

                    var otherRow = data.NewRow();
                    otherRow[0] = "No Active Locations";
                    data.Rows.Add(otherRow);



                    var locCount = data.Rows.Count;
                    var totalRotwRow = data.NewRow();
                    totalRotwRow[0] = "Total (Excluding China)";
                    totalRotwRow[1] = cRotwCount;
                    totalRotwRow[2] = newRotwCCount;
                    totalRotwRow[3] = dRotwCount;
                    totalRotwRow[4] = newRotwDCount;
                    data.Rows.Add(totalRotwRow);

                    var totalGlobalRow = data.NewRow();
                    totalGlobalRow[0] = "Total Globally";

                    totalGlobalRow[1] = cCount;
                    totalGlobalRow[2] = newCCount;
                    totalGlobalRow[3] = dCount;
                    totalGlobalRow[4] = newDCount;
                    data.Rows.Add(totalGlobalRow);

                    var rows = data.Rows.Cast<DataRow>().ToList();

                    int hidden = 0;
                    foreach (var row in rows)
                    {
                        if (row == otherRow || row == hiddenRow || row == totalRotwRow || row == totalGlobalRow) continue;
                        int totalCases = int.Parse(row.Field<string>("Cases"), System.Globalization.NumberStyles.AllowThousands);
                        int deaths = int.Parse(row.Field<string>("deaths") ?? "0", System.Globalization.NumberStyles.AllowThousands);
                        int totalRecovered = int.Parse(row.Field<string>("Recovered") ?? "0", System.Globalization.NumberStyles.AllowThousands);

                        if (totalCases == totalRecovered)
                        {
                            for (var i = 1; i < data.Columns.Count; i++)
                            {
                                var current = int.Parse(otherRow.Field<string>(i) ?? "0");
                                var additional = int.Parse(row.Field<string>(i) ?? "0");
                                otherRow[i] = current + additional;
                            }
                            hidden++;
                            data.Rows.Remove(row);
                        }
                    }
                    otherRow[0] = $"Non-Active Locations ({hidden})";
                    int max = 60;
                    rows = data.Rows.Cast<DataRow>().ToList();
                    rows.Reverse();
                    int otherCount = 0;
                    bool filterNew = false;
                    while (filterNew && rows.Count > max)
                    {
                        //hide rows without new cases or new deaths first
                        var row = rows.Skip(4).Where(x => x["new"].ToString() == "0" && x["new deaths"].ToString() == "0").FirstOrDefault();
                        if (row == null)
                        {
                            break;
                        }
                        for (var i = 1; i < data.Columns.Count; i++)
                        {
                            var current = int.Parse(hiddenRow.Field<string>(i) ?? "0");
                            var additional = int.Parse(row.Field<string>(i) ?? "0");
                            hiddenRow[i] = current + additional;
                        }
                        otherCount++;
                        data.Rows.Remove(row);
                        rows = data.Rows.Cast<DataRow>().ToList();
                        rows.Reverse();
                    }
                    while (rows.Count > max)
                    {
                        var row = rows.Skip(4).First();

                        for (var i = 1; i < data.Columns.Count; i++)
                        {
                            var current = int.Parse(hiddenRow.Field<string>(i) ?? "0");
                            var additional = int.Parse(row.Field<string>(i) ?? "0");
                            hiddenRow[i] = current + additional;
                        }
                        otherCount++;
                        data.Rows.Remove(row);
                        rows = data.Rows.Cast<DataRow>().ToList();
                        rows.Reverse();
                    }



                    hiddenRow[0] = $"Other Locations({otherCount})";

                    //data.AcceptChanges();
                    var doc = AsposeHelper.GetDataTableDocument(data);
                    var fileNamePng = $"cov-table-{DateTime.Now.ToFileTimeUtc()}.png";
                    var fileNameDocx = $"cov-table-{DateTime.Now.ToFileTimeUtc()}.docx";
                    IMedia media = null;
                    using (var ms = new System.IO.MemoryStream())
                    {
                        doc.Save(ms, Aspose.Words.SaveFormat.Png);
                        doc.Save(fileNamePng, Aspose.Words.SaveFormat.Png);
                        //doc.Save(fileNameDocx, Aspose.Words.SaveFormat.Docx);
                        media = Upload.UploadBinary(ms.ToArray());
                    }
                    var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
                    {
                        Medias = new List<IMedia>() { media }
                    };

                    Tweet.PublishTweet(formatted, tParams);
                    System.Threading.Thread.Sleep(120000);
                    pubbedAggr = true;
                }
                int diff = (int)DateTime.Now.Subtract(start).TotalMilliseconds;

                if (diff < sleep)
                {
                    var sleepTimeOut = sleep - diff;
                    System.Threading.Thread.Sleep(sleepTimeOut);
                }
                lastCTotal = cCount;
                lastDTotal = dCount;
                lastCtRowCount = cRotwCount;

            }
            //while (string.IsNullOrEmpty((txt=GetTxt())))
            //    System.Threading.Thread.Sleep(30000);
            //Console.WriteLine("[{DateTime.Now}]: txt");

            //Tweet.PublishTweet(txt);

        }

        private static bool HasBadData(List<ChangedStat> changes)
        {
            var withOld = changes.Where(x => x.Old != null);
            var withoutOld = changes.Where(x => x.Old == null);
            Func<ChangedStat, bool> badWithOldTotal = (x) =>
            {
                if (x.New.TotalCount > 100)
                {
                    return (x.New.TotalCount / x.Old.TotalCount) > 2;
                }
                return false;
            };
            Func<ChangedStat, bool> badWithOldDTotal = (x) =>
            {
                if (x.New.DailyDCount > 20)
                {
                    return (x.New.TotalDCount / x.Old.TotalDCount) > 2;
                }
                return false;
            };
            Func<ChangedStat, bool> badWithoutOldTotal = (x) => x.New.TotalCount > 100;

            Func<ChangedStat, bool> badWithoutOldDTotal = (x) => x.New.DailyDCount > 20;



            var result = withOld.Any(x => badWithOldTotal(x) || badWithOldDTotal(x))
                || withoutOld.Any(x => badWithoutOldTotal(x) || badWithoutOldDTotal(x));
            return result;

        }

        static DataTable GetUsaData()
        {
            bool success = false;
            var result = new DataTable();
            while (!success)
            {
                retry:
                try
                {


                    HtmlDocument htmlDoc = new HtmlDocument();
                    string url = "https://www.worldometers.info/coronavirus/usa-coronavirus";

                    string urlResponse = URLRequest(url);

                    //Convert the Raw HTML into an HTML Object
                    htmlDoc.LoadHtml(urlResponse);

                    var columns = new[] { "State", "Cases", "Sex", "Age", "Date", "Case #", "Location", "Source" };
                    foreach (var column in columns)
                        result.Columns.Add(column);

                    var table = htmlDoc.QuerySelectorAll("table").First();

                    var dataTableRows = table.QuerySelectorAll("tbody tr");


                    var lastState = "";
                    int stateCount = 0;


                    foreach (var tr in dataTableRows)
                    {

                        var cols = tr.QuerySelectorAll("td");

                        var state = cols.Count == 7 ? lastState :
                            string.IsNullOrEmpty(HtmlEntity.DeEntitize(cols[0].InnerText)?.Trim()) ? lastState : cols[0].InnerText;
                        if (!string.IsNullOrEmpty(lastState) && state != lastState)
                        {
                            var row = result.NewRow();
                            row["State"] = lastState;
                            row["Cases"] = stateCount;
                            result.Rows.Add(row);
                            stateCount = 1;
                            lastState = state;
                        }
                        else
                        {
                            lastState = state;
                            stateCount++;
                        }
                    }
                    var lastRow = result.NewRow();
                    lastRow["State"] = lastState;
                    lastRow["Cases"] = stateCount;
                    result.Rows.Add(lastRow);
                    stateCount = 0;
                    success = true;
                }
                catch (WebException ex)
                {
                    System.Threading.Thread.Sleep(10000);
                    goto retry;
                }
            }
            return result;
        }
        static DataTable GetData()
        {
            bool success = false;
            var result = new DataTable();
            while (!success)
            {
                retry:
                try
                {


                    HtmlDocument htmlDoc = new HtmlDocument();
                    string url = "https://www.worldometers.info/coronavirus/";

                    string urlResponse = URLRequest(url);

                    //Convert the Raw HTML into an HTML Object
                    htmlDoc.LoadHtml(urlResponse);

                    //for (var i = 0; i < 7; i++)
                    //    result.Columns.Add();
                    result.Columns.Add("Location");
                    result.Columns.Add("Cases");
                    result.Columns.Add("New");
                    result.Columns.Add("Deaths");
                    result.Columns.Add("New Deaths");
                    result.Columns.Add("Recovered");
                    result.Columns.Add("Serious/Critical");
                    var table = htmlDoc.QuerySelectorAll("table");

                    var dataTableRows = table.QuerySelectorAll("tbody tr");

                    foreach (var tr in dataTableRows)
                    {
                        var row = result.NewRow();
                        var cols = tr.QuerySelectorAll("td");
                        var loc = (cols.First().InnerText ?? "").Trim();
                        var active = int.Parse((cols[5].InnerText?.Trim() != "" ? cols[5].InnerText?.Trim() : null) ?? "0", System.Globalization.NumberStyles.AllowThousands);
                        //if (active == 0) continue;

                        if (loc.IndexOf("total", StringComparison.OrdinalIgnoreCase) > -1) continue;

                        row["Location"] = loc;

                        row["Cases"] = ParseInt(cols[1].InnerText);
                        row["New"] = ParseInt(cols[2].InnerText);
                        row["Deaths"] = ParseInt(cols[3].InnerText);
                        row["New Deaths"] = ParseInt(cols[4].InnerText);
                        row["Recovered"] = ParseInt(cols[5].InnerText);
                        row["Serious/Critical"] = ParseInt(cols[7].InnerText);
                        result.Rows.Add(row);

                    }
                    success = true;
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"[{DateTime.Now}]: {ex.Message}");
                    System.Threading.Thread.Sleep(10000);
                    goto retry;
                }
            }
            return result;
        }

        private static int ParseInt(string value)
        {
            value = (value ?? "").Trim().TrimStart('+');
            if (string.IsNullOrEmpty(value)) value = "0";
            return int.Parse(value, System.Globalization.NumberStyles.AllowThousands);
        }
        static string GetTxt()
        {
            //update
            HtmlDocument htmlDoc = new HtmlDocument();
            string url = "https://www.worldometers.info/coronavirus/";

            string urlResponse = URLRequest(url);

            //Convert the Raw HTML into an HTML Object
            htmlDoc.LoadHtml(urlResponse);

            //Find all title tags in the document
            /*
            <head>
                <title>Page Title</title>
            </head>
             */

            var table = htmlDoc.QuerySelectorAll("table").First();

            var dataTablRows = table.QuerySelectorAll("tbody tr");
            var cnCount = 0;
            var cnD = 0;
            var oD = 0;
            var oCount = 0;
            foreach (var tr in dataTablRows)
            {
                var cols = tr.QuerySelectorAll("td");
                var loc = (cols.First().InnerText ?? "").Trim();
                var cntText = (cols[1].InnerText ?? "").Trim();
                var dText = (cols[3].InnerText ?? "").Trim();
                if (string.IsNullOrEmpty(dText)) dText = "0";
                var cnt = int.Parse(cntText, System.Globalization.NumberStyles.AllowThousands);
                var d = int.Parse(dText, System.Globalization.NumberStyles.AllowThousands);
                if (loc == "China")
                {
                    cnCount = cnt;
                    cnD = d;
                }
                else
                {
                    oCount += cnt;
                    oD += d;
                }
            }
            var gCount = cnCount + oCount;
            var gDCount = oD + cnD;
            var formatted = $"Breaking: #CoronaVirus cases OUTSIDE of China: {oCount.ToString("N0")}.\n\nThere are now {gDCount.ToString("N0")} total deaths and {gCount.ToString("N0")} total #covid19 cases worldwide.\n\n#CoronaVirusOutbreak";
            var result = (oCount > 999) ? formatted : string.Empty;
            return result;
        }
        static string URLRequest(string url) { return new WebClient().DownloadString(url); }
    }
    class RTProcessor
    {

        internal static void Run()
        {
            System.Console.Title = $"[{DateTime.Now}] Runnint RT Processor";
            var authenticatedUser = User.GetAuthenticatedUser();

            Console.WriteLine(authenticatedUser);
            var stream = Stream.CreateFilteredStream();
            var current = User.GetAuthenticatedUser();
            var screenNames = System.IO.File.ReadAllText("RT.ini").Trim().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var ids = User.GetUsersFromScreenNames(screenNames).ToList();
            var allIds = ids.ToList();

            //ids.Clear();
            //ids.Add(current.Id);

            ids = ids.Distinct().ToList();
            ids.ForEach(id => stream.AddFollow(id));
            stream.MatchingTweetReceived += (sender, args) =>
            {
                ProcessReceived(args.Tweet);
            };


            stream.StartStreamMatchingAnyCondition();

        }

        private static void ProcessReceived(ITweet tweet)
        {
            //Console.WriteLine($"[{DateTime.Now}]: Recieved: @{tweet.CreatedBy.UserDTO.ScreenName}: { tweet.Text }");
            var tweetText = GetProcessedText(tweet);
            if (!string.IsNullOrEmpty(tweetText))
            {
                Tweet.PublishTweet(tweetText);
                //Console.WriteLine();
                //Console.WriteLine("".PadLeft(30, '='));
                //Console.WriteLine($"[{DateTime.Now}]: Published: {tweetText}");
                //Console.WriteLine("".PadLeft(30, '='));
                //Console.WriteLine();
            }
        }

        private static string GetProcessedText(ITweet tweet)
        {
            var source = tweet.CreatedBy.UserDTO.ScreenName;
            var result = "";
            bool isEmpty = true;
            switch (source)
            {
                case "BNODesk":
                    result = GetBNODeskText(tweet);
                    isEmpty = string.IsNullOrEmpty(result);
                    if (isEmpty) result = tweet.FullText;
                    break;
                case "WhiteHouse":
                    result = GetWhiteHouseText(tweet);
                    return result;
                case "NYGovCuomo":
                    result = GetCuomoText(tweet);
                    return result;
                default:
                    result = tweet.FullText;
                    break;
            }
            if (tweet.IsRetweet)
            {
                result = result.Substring(3);
                result = result.Substring(result.IndexOf(" ") + 1);
            }
            if (result.StartsWith("@") || result.StartsWith(".@"))
            {
                result = result.Substring(result.IndexOf(" ") + 1);
            }

            var rtHandle = $"RT @{tweet.CreatedBy.UserDTO.ScreenName}";
            if (result.StartsWith(rtHandle, StringComparison.OrdinalIgnoreCase)) result = result.Substring(rtHandle.Length);

            var proc = new TemplateProcessor(result);
            var temp = proc.GetProcessedText();
            result = temp;

            if (!isEmpty)
                Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now}] @{source}");
            Console.WriteLine($" => : {result}");
            Console.WriteLine("".PadLeft(10, '='));
            if (!isEmpty)
                Console.ForegroundColor = ConsoleColor.White;
            return isEmpty ? "" : result;
        }

        private static string GetCuomoText(ITweet tweet)
        {
            if (tweet.CreatedBy.UserDTO.ScreenName != "NYGovCuomo")
                return string.Empty;
            var text = tweet.FullText;
            //LIVE: Press Briefing with Coronavirus Task Force
            if (!text.ToLower().Contains("WATCH LIVE:".ToLower() ) || !text.ToLower().Contains("updates on #Coronavirus".ToLower()))
                return string.Empty;
            return "New York Gov. Cuomo " + text.Replace("Holding", "holds");
            //New York Gov. Cuomo press briefing with updates on #Coronavirus. WATCH LIVE:
        }
        private static string GetWhiteHouseText(ITweet tweet)
        {
            if (tweet.CreatedBy.UserDTO.ScreenName != "WhiteHouse")
                return string.Empty;
            var text = tweet.FullText;
            //LIVE: Press Briefing with Coronavirus Task Force
            if (!text.StartsWith("LIVE: ") || !text.Contains("Coronavirus Task Force"))
                return string.Empty;
            return text.Replace("Coronavirus", "U.S. #Coronavirus");
        }
        private static string GetBNODeskText(ITweet tweet)
        {
            var result = tweet.FullText;

            if (tweet.CreatedBy.UserDTO.ScreenName != "BNODesk" || tweet.QuotedTweet != null || tweet.IsRetweet || tweet.InReplyToStatusId != null)
                return string.Empty;
            var idx = result.LastIndexOf("http");
            if (idx > -1)
            {
                var tmpa = result.Substring(0, idx);
                var link = result.Substring(idx);
                var linkSpaceIdx = link.IndexOfAny(new[] { ' ', '\r', '\n' });
                if (linkSpaceIdx > -1 && linkSpaceIdx < link.Length)
                {
                    link = link.Substring(0, linkSpaceIdx);
                }
                var c = new WebClient();

                var req = HttpWebRequest.Create(link);
                var res = req.GetResponse();
                var uri = res.ResponseUri;

                string tmp = null;
                var bnoUri = new Uri("https://bnonews.com/");
                if (uri.Host != bnoUri.Host)
                {
                    tmp = result;
                }
                else
                {
                    tmp = tmpa;
                }


                result = tmp;
            }

            if (result.Length + " (BNO News)".Length < 280)
            {
                result += " (BNO News)";
            }

            return result;
        }
    }


    public class TemplateProcessConstants
    {
        const string ht = "#";
        const string corona = nameof(corona);
        const string virus = nameof(virus);
        const string outbreak = nameof(outbreak);
        public static string cvTag = "#CoronaVirus"; // $"{ht}{corona}{virus}";
        public static string[] cvTagCandidates = new[] { $"{corona}{virus}", $"{corona} {virus}", $"{virus}" };


        public const string cvIdTag = "#Covid19"; //"#covid19";
        public static string[] cvIdTagCandidates = new[] { "covid19", "covid-19", "virus" };

        public static string cOTag = "#CoronaVirusOutbreak"; //  $"{ht}{corona}{virus}{outbreak}";
        public static string[] cOTagCandidates = new[] { $"{corona} {virus} {outbreak}", $"{corona}{virus} {outbreak}", $"{virus} {outbreak}", $"{outbreak}" };
    }

    public class TemplateProcessor
    {

        private string rawText;
        const int MaxLength = 280;


        public TemplateProcessor(string text)
        {
            this.rawText = (text ?? "").Trim();

        }
        public string GetProcessedText()
        {
            AssureCVTag();
            return outputText();
        }
        private string outputText() => rawText;
        bool contains(string match) => rawText.IndexOf(match, StringComparison.OrdinalIgnoreCase) > -1;

        bool processNext()
        {
            return rawText.Length < MaxLength;
        }


        static string[] cvTagCandidates = TemplateProcessConstants.cvTagCandidates; // new[] { "cor", "cor vi" };

        bool replace(string tag, string candidate)
        {
            var idx = rawText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
            if (idx > -1 && notAHashTag(idx))
            {
                var temp = $"{rawText.Substring(0, idx)}{tag}{rawText.Substring(idx + candidate.Length)}";
                rawText = temp;
                return true;
            }
            return false;
        }

        private bool notAHashTag(int idx)
        {
            var result = true;
            try
            {
                //var spaceIdx = (idx > 0 ? rawText.Substring(0, idx - 1) : rawText).LastIndexOf(' ');
                var spaceIdx = rawText.LastIndexOf(' ', idx - 1, idx);
                var tail = rawText.Substring(spaceIdx + 1);
                var c = rawText[spaceIdx + 1];
                result = rawText[spaceIdx + 1] != '#';

                if (result)
                {
                    var lnIdx = rawText.LastIndexOf('\n', idx - 1, idx);
                    result = rawText[lnIdx + 1] != '#';
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }
            return result;
        }

        bool insertedNewLine = false;
        void assureTag(string tag, string[] candidates)
        {

            if (!contains(tag))
            {
                foreach (var candidate in candidates)
                {
                    if (replace(tag, candidate))
                    {
                        return;
                    }
                }

                if (!insertedNewLine)
                {
                    if ((rawText.Length + "\n\n").Length < MaxLength)
                        rawText += "\n\n";
                    insertedNewLine = true;
                    if (rawText.Length + tag.Length + 1 < 280)
                        rawText += "" + tag;
                }
                else
                {
                    if (rawText.Length + tag.Length + 1 < 280)
                        rawText += " " + tag;
                }
            }

        }
        void AssureCVTag()
        {
            assureTag(TemplateProcessConstants.cvTag, TemplateProcessConstants.cvTagCandidates);
            if (processNext())
            {
                AssureCVIdTag();
            }
        }


        void AssureCVIdTag()
        {
            assureTag(TemplateProcessConstants.cvIdTag, TemplateProcessConstants.cvIdTagCandidates);
            if (processNext())
            {
                AssureCOTag();
            }
        }

        void AssureCOTag()
        {
            assureTag(TemplateProcessConstants.cOTag, TemplateProcessConstants.cOTagCandidates);
        }
    }


    public class ChangedStat
    {
        public CvStat Old;
        public CvStat New;
        public ChangedStat(CvStat oldStat, CvStat newStat)
        {
            this.Old = oldStat ?? new CvStat();
            this.New = newStat;
        }

        public override string ToString()
        {
            int cdiff = New.TotalCount - Old.TotalCount;
            int ddiff = New.TotalDCount - Old.TotalDCount;

            int dailyC = New.DailyCount;
            int dailyD = New.DailyDCount;

            var tokens = new System.Collections.Generic.List<string>();
            tokens.Add($"{New.Loc} reports");
            if (cdiff > 0)
            {
                tokens.Add($"{cdiff.ToString("N0")} new");
                tokens.Add(cdiff == 1 ? "case" : "cases");
            }

            if (ddiff > 0)
            {
                if (cdiff > 0) tokens.Add("and");
                tokens.Add(ddiff.ToString("N0"));
                tokens.Add("new");
                tokens.Add(ddiff == 1 ? "death" : "deaths");
            }
            if (Old.TotalCount > 0 || Old.TotalDCount > 0)
            {
                tokens.Add($"bringing total confirmed cases there to {New.TotalCount.ToString("N0")}");

                if (New.TotalDCount > 0)
                {
                    tokens.Add($"and {New.TotalDCount.ToString("N0")} total");
                    tokens.Add(New.TotalDCount == 1 ? "death" : "deaths");
                }
            }
            else
            {
                //tokens.Add($"confirming the presence of the virus there for the first time");
            }
            var withTokens = new System.Collections.Generic.List<string>();
            if (Old.DailyCount > 0 || Old.DailyDCount > 0)
            {
                withTokens.Add("with");
                if (dailyC > 0)
                {

                    withTokens.Add($"{dailyC.ToString("N0")} new");
                    withTokens.Add(dailyC == 1 ? "case" : "cases");
                }
                if (dailyD > 0 && ddiff != dailyD)
                {
                    if (dailyC > 0) withTokens.Add("and");
                    withTokens.Add($"{dailyD.ToString("N0")} new");
                    withTokens.Add(dailyD == 1 ? "death" : "deaths");
                }
                withTokens.Add("reported today");
            }
            var result = $"{string.Join(" ", tokens)}";
            if (withTokens.Count > 0)
                result += $", {string.Join(" ", withTokens)}";
            result += ".";
            return result;
        }
        public DataTable ToDataTable()
        {
            var result = new DataTable();
            var d = new Dictionary<string, object>()
            {
                {$"{nameof(Old)}_{nameof(Old.DailyCount)}", Old.DailyCount },
                {$"{nameof(Old)}_{nameof(Old.DailyCount)}", Old.DailyDCount },
                {$"{nameof(Old)}_{nameof(Old.DailyCount)}", Old.Loc },
                {$"{nameof(Old)}_{nameof(Old.DailyCount)}", Old.TotalCount },
                {$"{nameof(Old)}_{nameof(Old.DailyCount)}", Old.TotalDCount },
                {$"{nameof(New)}_{nameof(Old.DailyCount)}", New.DailyCount },
                {$"{nameof(New)}_{nameof(Old.DailyCount)}", New.DailyDCount },
                {$"{nameof(New)}_{nameof(Old.DailyCount)}", New.Loc },
                {$"{nameof(New)}_{nameof(Old.DailyCount)}", New.TotalCount },
                {$"{nameof(New)}_{nameof(Old.DailyCount)}", New.TotalDCount },
            };

            foreach (var key in d.Keys)
            {
                result.Columns.Add(key);
            }
            var row = result.NewRow();
            foreach (var kvp in d)
            {
                row[kvp.Key] = kvp.Value;
            }
            result.Rows.Add(row);
            return result;
        }

    }
    public class CvCollection : IEquatable<CvCollection>, IComparable<CvCollection>
    {
        public System.Collections.Generic.List<CvStat> Stats = new List<CvStat>();


        public List<ChangedStat> GetChanges(CvCollection other)
        {
            var result = new List<ChangedStat>();
            //var dups = Stats.GroupBy(x => x.Loc).Where(x => x.Count() > 1).Select(x => x.ToArray()).ToList();
            //var otherDups = other.Stats.GroupBy(x => x.Loc).Where(x => x.Count() > 1).Select(x => x.ToArray()).ToList();
            //var d = Stats.Where(x => !string.IsNullOrEmpty(x.Loc)).ToDictionary(x => x.Loc, x => x);
            var otherD = other.Stats.Where(x => !String.IsNullOrEmpty(x.Loc))
                .ToLookup(x=> x.Loc).ToDictionary(x => x.Key, x => x.First());
            for (var i = 0; i < this.Stats.Count; i++)
            {
                var l = Stats[i].Loc;
                if (otherD.ContainsKey(l))
                {
                    var otherStat = otherD[l];
                    if (Stats[i] != otherStat)
                        result.Add(new ChangedStat(otherStat, Stats[i]));
                }
                else
                {
                    result.Add(new ChangedStat(null, Stats[i]));
                }



            }

            return result;
        }
        public static CvCollection Create(DataTable dt)
        {
            var result = new CvCollection();
            //var dt = Hermes.StringExtensions.DatatableFromCsv(source);
            var dataRows = dt.Rows.Cast<DataRow>().ToList();
            //tring[] rows = source.Trim().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);// new string[] { };
            var rows = dataRows.Select(row => row.ItemArray.Select(x => x.ToString()).ToArray());
            foreach (var row in rows)
            {
                string[] data = row;// row.Split(',');

                var stat = new CvStat();
                stat.Loc = (data[0] ?? "").Trim();
                stat.TotalCount = ParseInt(data[1]);
                stat.TotalDCount = ParseInt(data[3]);
                stat.DailyCount = ParseInt(data[2]);
                stat.DailyDCount = ParseInt(data[4]);
                result.Stats.Add(stat);
            }

            return result;
        }

        private static int ParseInt(string value)
        {
            value = (value ?? "").Trim();
            if (string.IsNullOrEmpty(value)) value = "0";
            return int.Parse(value, System.Globalization.NumberStyles.AllowThousands);
        }

        public int CompareTo(CvCollection other)
        {
            var result = this.Stats.Count.CompareTo(other.Stats.Count);
            if (result == 0)
            {
                for (var i = 0; result == 0 && i < this.Stats.Count; i++)
                    result = Stats[i].CompareTo(other.Stats[i]);
            }
            return result;
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as CvCollection);
        }

        public bool Equals(CvCollection other)
        {
            if (other is null) return false;
            if (Stats.Count != other.Stats.Count) return false;
            for (var i = 0; i < Stats.Count; i++)
            {
                if (Stats[i] != other.Stats[i]) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return -1464643476 + EqualityComparer<List<CvStat>>.Default.GetHashCode(Stats);
        }

        public static bool operator ==(CvCollection left, CvCollection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CvCollection left, CvCollection right)
        {
            return !(left == right);
        }
    }
    public class CvStat : IEquatable<CvStat>, IComparable<CvStat>
    {
        public string Loc { get; set; }
        public int DailyCount { get; set; }
        public int TotalCount { get; set; }
        public int DailyDCount { get; set; }
        public int TotalDCount { get; set; }

        public int CompareTo(CvStat other)
        {
            int result = TotalCount.CompareTo(other.TotalCount);
            if (result == 0)
                result = Loc.CompareTo(other.Loc);
            if (result == 0)
                result = DailyCount.CompareTo(other.DailyCount);
            if (result == 0)
                result = TotalDCount.CompareTo(other.TotalDCount);
            if (result == 0)
                result = DailyDCount.CompareTo(other.DailyDCount);
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CvStat);
        }

        public bool Equals(CvStat other)
        {
            var result = other != null &&
                   Loc == other.Loc &&
                   //DailyCount == other.DailyCount &&
                   TotalCount == other.TotalCount;
            //DailyDCount == other.DailyDCount &&
            //TotalDCount == other.TotalDCount;
            return result;
        }

        public override int GetHashCode()
        {
            var hashCode = 465562093;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Loc);
            hashCode = hashCode * -1521134295 + DailyCount.GetHashCode();
            hashCode = hashCode * -1521134295 + TotalCount.GetHashCode();
            hashCode = hashCode * -1521134295 + DailyDCount.GetHashCode();
            hashCode = hashCode * -1521134295 + TotalDCount.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(CvStat left, CvStat right)
        {
            return EqualityComparer<CvStat>.Default.Equals(left, right);
        }

        public static bool operator !=(CvStat left, CvStat right)
        {
            return !(left == right);
        }
    }
}
