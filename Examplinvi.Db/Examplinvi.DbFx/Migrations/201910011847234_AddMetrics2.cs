namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMetrics2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Metrics", "Url", c => c.String(nullable: false, maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Metrics", "Url", c => c.String(maxLength: 100));
        }
    }
}
