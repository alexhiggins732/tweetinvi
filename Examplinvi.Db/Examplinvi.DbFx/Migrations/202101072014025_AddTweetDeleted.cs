namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTweetDeleted : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DbTweets", "Deleted", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DbTweets", "Deleted");
        }
    }
}
