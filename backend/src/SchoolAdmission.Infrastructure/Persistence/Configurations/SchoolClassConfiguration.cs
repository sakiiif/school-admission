using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> b)
    {
        b.ToTable("Classes");

        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasMaxLength(64);

        b.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(SchoolClass.MaxNameLength);

        b.Property(c => c.TotalSeats).IsRequired();

        b.Property(c => c.CreatedAt)
            .HasColumnType("timestamp with time zone");

        // DB-level safety net: the domain already rejects totalSeats <= 0
        b.ToTable(t => t.HasCheckConstraint("CK_Classes_TotalSeats_Positive", "\"TotalSeats\" > 0"));
    }
}