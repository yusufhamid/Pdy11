namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Inheritance : DbMigration
    {
        // UNDONE: previous vers of Up before implementing inheritance for
        // Student : Person AND Instructor : Person, resulting in TPH
        // Person table in the db 

        //public override void Up()
        //{
        //    RenameTable(name: "dbo.Instructor", newName: "Person");
        //    AddColumn("dbo.Person", "EmailAddress", c => c.String());
        //    AddColumn("dbo.Person", "EnrollmentDate", c => c.DateTime());
        //    AddColumn("dbo.Person", "Discriminator", c => c.String(nullable: false, maxLength: 128));
        //    AlterColumn("dbo.Person", "HireDate", c => c.DateTime());
        //    DropTable("dbo.Student");
        //}

        public override void Up()
        {
            // Drop foreign keys and indexes that point to tables we're going to drop.
            DropForeignKey("dbo.Enrollment", "StudentID", "dbo.Student");
            DropIndex("dbo.Enrollment", new[] { "StudentID" });

            RenameTable(name: "dbo.Instructor", newName: "Person");
            AddColumn("dbo.Person", "EmailAddress", c => c.String(nullable: true, maxLength: 128));
            AddColumn("dbo.Person", "EnrollmentDate", c => c.DateTime());
            AddColumn("dbo.Person", "Discriminator", c => c.String(nullable: false, maxLength: 128, defaultValue: "Instructor"));
            AlterColumn("dbo.Person", "HireDate", c => c.DateTime());
            AddColumn("dbo.Person", "OldId", c => c.Int(nullable: true));

            // Copy existing Student data into new Person table.
            Sql("INSERT INTO dbo.Person (LastName, FirstName, EmailAddress, HireDate, EnrollmentDate, Discriminator, OldId) SELECT LastName, FirstName, EmailAddress, null AS HireDate, EnrollmentDate, 'Student' AS Discriminator, ID AS OldId FROM dbo.Student");

            // Fix up existing relationships to match new PK's.
            Sql("UPDATE dbo.Enrollment SET StudentId = (SELECT ID FROM dbo.Person WHERE OldId = Enrollment.StudentId AND Discriminator = 'Student')");

            // Remove temporary key
            DropColumn("dbo.Person", "OldId");

            DropTable("dbo.Student");

            // Re-create foreign keys and indexes pointing to new table.
            AddForeignKey("dbo.Enrollment", "StudentID", "dbo.Person", "ID", cascadeDelete: true);
            CreateIndex("dbo.Enrollment", "StudentID");
        }


        public override void Down()
        {
            CreateTable(
                "dbo.Student",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LastName = c.String(maxLength: 50),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        EmailAddress = c.String(),
                        EnrollmentDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            AlterColumn("dbo.Person", "HireDate", c => c.DateTime(nullable: false));
            DropColumn("dbo.Person", "Discriminator");
            DropColumn("dbo.Person", "EnrollmentDate");
            DropColumn("dbo.Person", "EmailAddress");
            RenameTable(name: "dbo.Person", newName: "Instructor");
        }
    }
}
