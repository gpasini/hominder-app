using System.Text.Json;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;
using Hominder.Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hominder.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceTaskConfiguration : IEntityTypeConfiguration<MaintenanceTask>
{
    private static readonly JsonSerializerOptions PolicyJsonOptions = CreatePolicyJsonOptions();

    public void Configure(EntityTypeBuilder<MaintenanceTask> builder)
    {
        builder.ToTable("maintenance_tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id)
            .HasConversion(new MaintenanceTaskIdConverter())
            .ValueGeneratedNever();

        builder.Property(task => task.Title).IsRequired();
        builder.Property(task => task.Notes);

        builder.Property(task => task.AssigneeId)
            .HasConversion(new HouseholdMemberIdConverter());

        builder.Property(task => task.Policy)
            .HasColumnType("jsonb")
            .HasConversion(
                policy => JsonSerializer.Serialize(policy, PolicyJsonOptions),
                json => JsonSerializer.Deserialize<RecurrencePolicy>(json, PolicyJsonOptions)!);

        builder.OwnsMany(task => task.Completions, completions =>
        {
            completions.ToTable("maintenance_task_completions");
            completions.WithOwner().HasForeignKey("MaintenanceTaskId");
            completions.Property<int>("Id");
            completions.HasKey("Id");
            completions.Property(completion => completion.CompletedOn);
            completions.Property(completion => completion.CompletedBy)
                .HasConversion(new HouseholdMemberIdConverter());
        });

        builder.Navigation(task => task.Completions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static JsonSerializerOptions CreatePolicyJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new RecurrencePolicyJsonConverter());
        return options;
    }
}
