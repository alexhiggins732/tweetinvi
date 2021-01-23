using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi.Models;

namespace Examplinvi.DbFx.Models
{
    public class Metric
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public long TweetId { get; set; }

        public long? RetweetId { get; set; }
        public long? QuotedTweetId { get; set; }
        public long? ReplyToTweetId { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Url { get; set; }

        //private ITweet tweet;
        public Metric() { }
        public Metric(ITweet tweet)
        {
            //this.tweet = tweet;
            this.UserId = tweet.CreatedBy.Id;
            this.TweetId = tweet.Id;
            this.RetweetId = tweet.RetweetedTweet?.Id;
            this.QuotedTweetId = tweet.QuotedStatusId;
            this.ReplyToTweetId = tweet.InReplyToStatusId;
            this.CreatedAt = tweet.CreatedAt;
            this.Url = tweet.Url;
        }
        public Metric(long tweetId, long userId, DateTime createdAt, bool deleted)
        {
            this.UserId = userId;
            this.TweetId = tweetId;
            this.CreatedAt = createdAt;
            this.Deleted = deleted;

        }
    }
}
