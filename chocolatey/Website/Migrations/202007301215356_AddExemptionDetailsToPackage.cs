namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddExemptionDetailsToPackage : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "ExemptedFromValidatorById", c => c.Int());
            AddColumn("Packages", "ExemptedFromScannerById", c => c.Int());
            AlterColumn("Packages", "ExemptedFromScannerReason", c => c.String(maxLength: 500));
            AlterColumn("Packages", "ExemptedFromValidatorReason", c => c.String(maxLength: 500));
            AddForeignKey("Packages", "ExemptedFromValidatorById", "Users", "Key");
            AddForeignKey("Packages", "ExemptedFromScannerById", "Users", "Key");
            CreateIndex("Packages", "ExemptedFromValidatorById");
            CreateIndex("Packages", "ExemptedFromScannerById");
        }

        public override void Down()
        {
            DropIndex("Packages", new[] { "ExemptedFromScannerById" });
            DropIndex("Packages", new[] { "ExemptedFromValidatorById" });
            DropForeignKey("Packages", "ExemptedFromScannerById", "Users");
            DropForeignKey("Packages", "ExemptedFromValidatorById", "Users");
            AlterColumn("Packages", "ExemptedFromValidatorReason", c => c.String());
            AlterColumn("Packages", "ExemptedFromScannerReason", c => c.String());
            DropColumn("Packages", "ExemptedFromScannerById");
            DropColumn("Packages", "ExemptedFromValidatorById");
        }
    }
}
