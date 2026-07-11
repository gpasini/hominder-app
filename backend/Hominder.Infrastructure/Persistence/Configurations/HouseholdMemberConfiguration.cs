using Hominder.Domain.Household;
using Hominder.Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hominder.Infrastructure.Persistence.Configurations;

public sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("household_members");

        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id)
            .HasConversion(new HouseholdMemberIdConverter())
            .ValueGeneratedNever();

        builder.Property(member => member.Name).IsRequired();
    }
}
