namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DbTweets",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        UserId = c.Long(nullable: false),
                        Text = c.String(maxLength: 260),
                        CreatedAt = c.DateTime(nullable: false),
                        ReplyToId = c.Long(),
                        QuotedId = c.Long(),
                        RetweetId = c.Long(),
                        RetweetCount = c.Int(nullable: false),
                        ReplyCount = c.Int(),
                        LikeCount = c.Int(nullable: false),
                        Favorited = c.Boolean(nullable: false),
                        Retweeted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.DbTweetMedias",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        DisplayURL = c.String(maxLength: 1000),
                        ExpandedURL = c.String(maxLength: 1000),
                        MediaURL = c.String(maxLength: 1000),
                        MediaURLHttps = c.String(maxLength: 1000),
                        MediaType = c.String(maxLength: 100),
                        TweetId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.DbTweets", t => t.TweetId, cascadeDelete: true)
                .Index(t => t.TweetId);
            
            CreateTable(
                "dbo.DbVideoDetails",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        DurationInMilliseconds = c.Long(nullable: false),
                        Bitrate = c.Int(nullable: false),
                        ContentType = c.String(maxLength: 100),
                        URL = c.String(maxLength: 1000),
                        TweetMediaId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.DbTweetMedias", t => t.TweetMediaId, cascadeDelete: true)
                .Index(t => t.TweetMediaId);
            
            CreateTable(
                "dbo.DbUsers",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        ScreenName = c.String(maxLength: 100),
                        Name = c.String(maxLength: 100),
                        Description = c.String(maxLength: 260),
                        CreatedAt = c.DateTime(nullable: false),
                        FavoritesCount = c.Int(),
                        ListedCount = c.Int(),
                        StatusesCount = c.Int(nullable: false),
                        FollowersCount = c.Int(nullable: false),
                        FollowingCount = c.Int(nullable: false),
                        Language = c.Int(),
                        Verified = c.Boolean(nullable: false),
                        Following = c.Boolean(nullable: false),
                        FollowsMe = c.Boolean(),
                        FollowedDate = c.DateTime(),
                        UnFollowedDate = c.DateTime(),
                        FollowedMeDate = c.DateTime(),
                        UnFollowedMeDate = c.DateTime(),
                        Protected = c.Boolean(nullable: false),
                        WhiteListed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DbVideoDetails", "TweetMediaId", "dbo.DbTweetMedias");
            DropForeignKey("dbo.DbTweetMedias", "TweetId", "dbo.DbTweets");
            DropIndex("dbo.DbVideoDetails", new[] { "TweetMediaId" });
            DropIndex("dbo.DbTweetMedias", new[] { "TweetId" });
            DropTable("dbo.DbUsers");
            DropTable("dbo.DbVideoDetails");
            DropTable("dbo.DbTweetMedias");
            DropTable("dbo.DbTweets");
        }
    }
}
