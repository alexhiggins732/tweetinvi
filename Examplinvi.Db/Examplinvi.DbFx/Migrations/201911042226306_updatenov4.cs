namespace Examplinvi.DbFx.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatenov4 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DbUsers", "IsMyUser", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DbUsers", "IsMyUser");
        }
    }
}
