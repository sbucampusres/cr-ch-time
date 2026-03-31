using Microsoft.EntityFrameworkCore;
using CRCHTime.Models.Entities;

namespace CRCHTime.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // CardSwipe domain entities
    public DbSet<Visit> Visits { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<TimesheetEntry> TimesheetEntries { get; set; }
    public DbSet<SwipeEntry> SwipeEntries { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<IdAssociation> IdAssociations { get; set; }
    public DbSet<ShiftCategory> ShiftCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Visit entity
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.ToTable("WS_FCVISITS");
            entity.HasKey(e => e.Id);
        });

        // Configure Staff entity
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("WS_FCSTAFF");
            // Composite key: NETID + APPLICATION
            entity.HasKey(e => new { e.NetId, e.Application });

            // Explicitly ignore the Department navigation property to prevent
            // EF Core from creating a shadow foreign key (DepartmentDeptId)
            // The DEPT_ID column is a string and doesn't have a real FK relationship
            entity.Ignore(e => e.Department);
        });

        // Configure TimesheetEntry entity
        modelBuilder.Entity<TimesheetEntry>(entity =>
        {
            entity.ToTable("WS_FCSTAFFWORKLOG");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Department)
                  .WithMany()
                  .HasForeignKey(e => e.DepartmentId)
                  .IsRequired(false);
        });

        // Configure SwipeEntry entity
        modelBuilder.Entity<SwipeEntry>(entity =>
        {
            entity.ToTable("WS_FCSWIPES");
            entity.HasKey(e => e.Id);
        });

        // Configure Building entity
        modelBuilder.Entity<Building>(entity =>
        {
            entity.ToTable("WS_FC_BUILDING");
            entity.HasKey(e => e.BuildingId);

            // Configure boolean as int for Oracle
            entity.Property(e => e.Inactive).HasConversion<int>();
        });

        // Configure Department entity
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("WS_FC_DEPARTMENTS");
            entity.HasKey(e => e.DeptId);

            // Configure boolean as int for Oracle
            entity.Property(e => e.Inactive).HasConversion<int>();

            // Explicitly ignore the Staff navigation collection to prevent
            // EF Core from inferring a relationship with Staff entity
            entity.Ignore(e => e.Staff);
        });

        // Configure Company entity
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("WS_FC_COMPANY");
            entity.HasKey(e => e.CompanyId);
        });

        // Configure IdAssociation entity
        modelBuilder.Entity<IdAssociation>(entity =>
        {
            entity.ToTable("WS_FCIDASSOCNAME");
            entity.HasKey(e => e.SBUID);
        });

        // Configure ShiftCategory entity
        modelBuilder.Entity<ShiftCategory>(entity =>
        {
            entity.ToTable("WS_CR_CS_SHIFT_CATEGORIES");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsActive).HasConversion<int>();
        });
    }
}
