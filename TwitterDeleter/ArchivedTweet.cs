namespace TwitterDeleter.Archived
{

    public class ArchivedTweet
    {
        public Tweet tweet { get; set; }
    }

    public class Tweet
    {
        public bool retweeted { get; set; }
        public string source { get; set; }
        public Entities entities { get; set; }
        public string[] display_text_range { get; set; }
        public string favorite_count { get; set; }
        public string in_reply_to_status_id_str { get; set; }
        public string id_str { get; set; }
        public string in_reply_to_user_id { get; set; }
        public bool truncated { get; set; }
        public string retweet_count { get; set; }
        public string id { get; set; }
        public string in_reply_to_status_id { get; set; }
        public string created_at { get; set; }
        public bool favorited { get; set; }
        public string full_text { get; set; }
        public string lang { get; set; }
        public string in_reply_to_screen_name { get; set; }
        public string in_reply_to_user_id_str { get; set; }
    }

    public class Entities
    {
        public object[] hashtags { get; set; }
        public object[] symbols { get; set; }
        public User_Mentions[] user_mentions { get; set; }
        public object[] urls { get; set; }
    }

    public class User_Mentions
    {
        public string name { get; set; }
        public string screen_name { get; set; }
        public string[] indices { get; set; }
        public string id_str { get; set; }
        public string id { get; set; }
    }

}
