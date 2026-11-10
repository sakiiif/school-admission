using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> b)
    {
        b.ToTable("Holds");

        b.HasKey(h => h.Id);
        b.Property(h => h.Id).HasMaxLength(64);

        b.Property(h => h.ClassId).IsRequired().HasMaxLength(64);

        b.Property(h => h.StudentId)
            .IsRequired()
            .HasMaxLength(17)
            .IsFixedLength();

        // Stored as text: "Held", "Confirmed", "Expired", "Cancelled"
        b.Property(h => h.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        b.Property(h => h.CreatedAt).HasColumnType("timestamp with time zone");
        b.Property(h => h.ExpiresAt).HasColumnType("timestamp with time zone");
        b.Property(h => h.ConfirmedAt).HasColumnType("timestamp with time zone");

        // Relationships
        b.HasOne<SchoolClass>()
            .WithMany()
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // one active (Held) or Confirmed application per student per class.
        // Cancelled/Expired rows are ignored, so re-applying is allowed.
        b.HasIndex(h => new { h.ClassId, h.StudentId })
            .IsUnique()
            .HasDatabaseName("UX_Holds_Class_Student_Active")
            .HasFilter("\"Status\" IN ('Held','Confirmed')");

        // Seat counting: WHERE ClassId = ? AND Status = ? AND ExpiresAt > now()
        b.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt })
            .HasDatabaseName("IX_Holds_Class_Status_Expires");

        // GET /me/holds
        b.HasIndex(h => h.StudentId)
            .HasDatabaseName("IX_Holds_Student");
    }
}