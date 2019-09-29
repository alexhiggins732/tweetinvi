namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DbMediaManyTweets : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.DbTweetMedias", "TweetId", "dbo.DbTweets");
            DropIndex("dbo.DbTweetMedias", new[] { "TweetId" });
            CreateTable(
                "dbo.DbTweetMediaDbTweets",
                c => new
                    {
                        DbTweetMedia_Id = c.Long(nullable: false),
                        DbTweet_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.DbTweetMedia_Id, t.DbTweet_Id })
                .ForeignKey("dbo.DbTweetMedias", t => t.DbTweetMedia_Id, cascadeDelete: true)
                .ForeignKey("dbo.DbTweets", t => t.DbTweet_Id, cascadeDelete: true)
                .Index(t => t.DbTweetMedia_Id)
                .Index(t => t.DbTweet_Id);
            
            DropColumn("dbo.DbTweetMedias", "TweetId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.DbTweetMedias", "TweetId", c => c.Long(nullable: false));
            DropForeignKey("dbo.DbTweetMediaDbTweets", "DbTweet_Id", "dbo.DbTweets");
            DropForeignKey("dbo.DbTweetMediaDbTweets", "DbTweetMedia_Id", "dbo.DbTweetMedias");
            DropIndex("dbo.DbTweetMediaDbTweets", new[] { "DbTweet_Id" });
            DropIndex("dbo.DbTweetMediaDbTweets", new[] { "DbTweetMedia_Id" });
            DropTable("dbo.DbTweetMediaDbTweets");
            CreateIndex("dbo.DbTweetMedias", "TweetId");
            AddForeignKey("dbo.DbTweetMedias", "TweetId", "dbo.DbTweets", "Id", cascadeDelete: true);
        }
    }
}
