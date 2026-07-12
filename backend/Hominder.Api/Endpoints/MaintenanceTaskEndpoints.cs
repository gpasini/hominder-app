using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Commands;
using Hominder.Application.Maintenance.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hominder.Api.Endpoints;

public sealed record CreateMaintenanceTaskRequest(
    string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

public sealed record UpdateMaintenanceTaskRequest(
    string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

public sealed record MarkMaintenanceTaskDoneRequest(
    DateOnly CompletedOn, Guid CompletedBy, DateOnly? NextDueOverride);

public static class MaintenanceTaskEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks");

        group.MapGet("", async Task<Ok<IReadOnlyList<MaintenanceTaskView>>> (
            ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetMaintenanceTasksQuery(), cancellationToken)));

        group.MapPost("", async (
            CreateMaintenanceTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var id = await sender.Send(
                new CreateMaintenanceTaskCommand(request.Title, request.Notes, request.Policy, request.AssigneeId),
                cancellationToken);
            return Results.Created($"/api/tasks/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateMaintenanceTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new UpdateMaintenanceTaskCommand(id, request.Title, request.Notes, request.Policy, request.AssigneeId),
                cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteMaintenanceTaskCommand(id), cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/completions", async (
            Guid id, MarkMaintenanceTaskDoneRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new MarkMaintenanceTaskDoneCommand(id, request.CompletedOn, request.CompletedBy, request.NextDueOverride),
                cancellationToken);
            return Results.NoContent();
        });

        return routes;
    }
}
