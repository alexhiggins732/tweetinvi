namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DbMediaManyTweets2 : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.DbTweetMediaDbTweets", newName: "DbTweetDbTweetMedias");
            DropPrimaryKey("dbo.DbTweetDbTweetMedias");
            AddPrimaryKey("dbo.DbTweetDbTweetMedias", new[] { "DbTweet_Id", "DbTweetMedia_Id" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.DbTweetDbTweetMedias");
            AddPrimaryKey("dbo.DbTweetDbTweetMedias", new[] { "DbTweetMedia_Id", "DbTweet_Id" });
            RenameTable(name: "dbo.DbTweetDbTweetMedias", newName: "DbTweetMediaDbTweets");
        }
    }
}
