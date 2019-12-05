namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMetrics1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Metrics", "ReplyToTweetId", c => c.Long());
            AddColumn("dbo.Metrics", "Url", c => c.String(maxLength: 100));
            DropColumn("dbo.Metrics", "RepylToweetId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Metrics", "RepylToweetId", c => c.Long());
            DropColumn("dbo.Metrics", "Url");
            DropColumn("dbo.Metrics", "ReplyToTweetId");
        }
    }
}
