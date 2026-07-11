using Hominder.Application.Household.Commands;
using Hominder.Application.Household.Queries;
using MediatR;

namespace Hominder.Api.Endpoints;

public sealed record CreateHouseholdMemberRequest(string Name);

public static class HouseholdMemberEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdMemberEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/members");

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetHouseholdMembersQuery(), cancellationToken)));

        group.MapPost("", async (
            CreateHouseholdMemberRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var id = await sender.Send(new CreateHouseholdMemberCommand(request.Name), cancellationToken);
            return Results.Created($"/api/members/{id}", new { id });
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteHouseholdMemberCommand(id), cancellationToken);
            return Results.NoContent();
        });

        return routes;
    }
}
