namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddScanStatusToModerationProcess : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "PackageScanFlagResult", c => c.String(maxLength: 50));
            AddColumn("Packages", "ExemptedFromValidatorReason", c => c.String());
            AddColumn("Packages", "ExemptedFromScannerReason", c => c.String());
        }

        public override void Down()
        {
            DropColumn("Packages", "ExemptedFromScannerReason");
            DropColumn("Packages", "ExemptedFromValidatorReason");
            DropColumn("Packages", "PackageScanFlagResult");
        }
    }
}
