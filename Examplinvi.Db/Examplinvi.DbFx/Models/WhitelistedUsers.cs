using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Models.Entities.ExtendedEntities;
//using Tweetinvi.Logic.DTO;

namespace Examplinvi.DbFx.Models
{
    public static class DTOExtensions
    {
        public static DbUser ToDbUser(this Tweetinvi.Models.DTO.IUserDTO dto, bool? followsMe = null, bool whiteListed = false)
        {
            return DbUser.ToDbUser(dto, followsMe, whiteListed);
        }
        public static DbUser ToDbUser(this Tweetinvi.Logic.DTO.UserDTO dto, bool? followsMe = null, bool whiteListed = false)
        {
            return DbUser.ToDbUser(dto, followsMe, whiteListed);
        }

        public static DbUser ToDbUser(this IUser dto, bool? followsMe = null, bool whiteListed = false)
        {
            return DbUser.ToDbUser(dto.UserDTO, followsMe, whiteListed);
        }

        //public static DbTweet ToDbTweet(this Tweetinvi.Models.DTO.ITweetDTO dto)
        //{
        //    return DbTweet.ToDbTweet(dto);
        //}
        //public static DbTweet ToDbTweet(this Tweetinvi.Logic.DTO.TweetDTO dto)
        //{
        //    return DbTweet.ToDbTweet(dto);
        //}
        public static DbTweet ToDbTweet(this ITweet dto)
        {
            return DbTweet.ToDbTweet(dto);
        }


    }
    public interface IDbEntity
    {
        long Id { get; }
    }
    public class DbUser: IDbEntity
    {
        public long Id { get; set; }
        public string ScreenName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FavoritesCount { get; set; }
        public int? ListedCount { get; set; }
        public int StatusesCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public Tweetinvi.Models.Language? Language { get; set; }
        public bool Verified { get; set; }
        public bool Following { get; set; }
        public bool? FollowsMe { get; set; }
        public DateTime? FollowedDate { get; set; }
        public DateTime? UnFollowedDate { get; set; }
        public DateTime? FollowedMeDate { get; set; }
        public DateTime? UnFollowedMeDate { get; set; }
        public bool Protected { get; set; }
        public bool WhiteListed { get; set; }
        public static DbUser ToDbUser(Tweetinvi.Models.DTO.IUserDTO userDto, bool? followsMe = null, bool whiteListed = false)
        {
            var dbUser = new DbUser
            {
                Id = userDto.Id,
                ScreenName = userDto.ScreenName,
                Name = userDto.Name,
                Description = userDto.Description,
                CreatedAt = userDto.CreatedAt,
                FavoritesCount = userDto.FavoritesCount,
                ListedCount = userDto.ListedCount,
                StatusesCount = userDto.StatusesCount,
                FollowersCount = userDto.FollowersCount,
                FollowingCount = userDto.FriendsCount,
                Language = userDto.Language,
                Verified = userDto.Verified,
                Protected = userDto.Protected,
                Following = userDto.Following,
                FollowedDate = userDto.Following ? (DateTime?)DateTime.Now : null,
                FollowsMe = followsMe,
                FollowedMeDate = (followsMe != null && followsMe.Value) ? (DateTime?)DateTime.Now : null,
                WhiteListed = whiteListed
            };

            return dbUser;
        }
    }

    public class DbTweet
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? ReplyToId { get; set; }
        public long? QuotedId { get; set; }
        public long? RetweetId { get; set; }


        //public virtual DbUser User { get; set; }
        public List<DbTweetMedia> Media { get; set; }

        //public virtual DbTweet ReplyTo { get; set; }
        //public virtual DbTweet Quoted { get; set; }
        //public virtual DbTweet Retweet { get; set; }
        public int RetweetCount { get; set; }
        public int? ReplyCount { get; set; }
        public int LikeCount { get; set; }
        public bool Favorited { get; set; }
        public bool Retweeted { get; set; }
        public bool Deleted { get; set; }
        public static DbTweet ToDbTweet(ITweet tweetDto)
        {
            var dbTweet = new DbTweet
            {
                Id = tweetDto.Id,
                UserId = tweetDto.CreatedBy?.Id ?? 0,
                Text = tweetDto.Text,
                CreatedAt = tweetDto.CreatedAt,
                ReplyToId = tweetDto.InReplyToStatusId,
                QuotedId = tweetDto.QuotedStatusId,
                RetweetId = tweetDto.RetweetedTweet?.Id,
                RetweetCount = tweetDto.RetweetCount,
                ReplyCount = tweetDto.ReplyCount,
                LikeCount = tweetDto.FavoriteCount,
                Favorited = tweetDto.Favorited,
                Retweeted = tweetDto.Retweeted,
            };

            dbTweet.Media = DbTweetMedia.ToDbTweetMedia(dbTweet, tweetDto);
            return dbTweet;
        }
    }

    public class DbTweetMedia
    {
        public long Id { get; set; }
        public string DisplayURL { get; set; }
        public string ExpandedURL { get; set; }
        public string MediaURL { get; set; }
        public string MediaURLHttps { get; set; }
        public string MediaType { get; set; }

        public virtual List<DbTweet> Tweet { get; set; }
        public virtual List<DbVideoDetails> VideoDetails { get; set; }
        internal static List<DbTweetMedia> ToDbTweetMedia(DbTweet dbTweet, ITweet tweetDto)
        {
            var dbMedia = new List<DbTweetMedia>();
            var mediaList = tweetDto.Media;

            if (mediaList != null)
            {
                foreach (var media in mediaList)
                {
                    dbMedia.Add(ToDbTweetMedia(dbTweet, media));
                }
            }
            return dbMedia;
        }

        private static DbTweetMedia ToDbTweetMedia(DbTweet dbTweet, IMediaEntity media)
        {
            var dbMedia = new DbTweetMedia
            {
                Id = (long)media.Id,
                DisplayURL = media.DisplayURL,
                ExpandedURL = media.ExpandedURL,
                MediaURL = media.MediaURL,
                MediaURLHttps = media.MediaURLHttps,
                MediaType = media.MediaType,
                Tweet = (new[] { dbTweet }).ToList()
            };
            if (media.VideoDetails != null)
            {
                dbMedia.VideoDetails = DbVideoDetails.ToDbVideoDetails(dbMedia, media.VideoDetails);
            }
            return dbMedia;
        }
    }

    public class DbVideoDetails
    {
        public long Id { get; set; }
        public long DurationInMilliseconds { get; set; }
        public int Bitrate { get; private set; }

        public string ContentType { get; set; }

        public string URL { get; private set; }
        public long TweetMediaId { get; set; }
        public virtual DbTweetMedia TweetMedia { get; set; }
    

        internal static List<DbVideoDetails> ToDbVideoDetails(DbTweetMedia dbMedia, IVideoInformationEntity videoInfo)
        {
            var videoList = new List<DbVideoDetails>();
            if (videoInfo != null)
            {
                foreach (var item in videoInfo.Variants)
                {

                    var videoDetails = new DbVideoDetails
                    {
                        DurationInMilliseconds = videoInfo.DurationInMilliseconds,
                        URL = item.URL,
                        ContentType = item.ContentType,
                        Bitrate = item.Bitrate,
                        TweetMedia = dbMedia
                    };
                    videoList.Add(videoDetails);
                }
            }
            return videoList;
        }
    }
}
