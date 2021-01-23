namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateTweetTextLength : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.DbTweets", "Text", c => c.String(maxLength: 300));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.DbTweets", "Text", c => c.String(maxLength: 260));
        }
    }
}
