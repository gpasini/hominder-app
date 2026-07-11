using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance;

public sealed class DueWindow : ValueObject
{
    public DueWindow(DateOnly openDate, DateOnly dueDate)
    {
        if (openDate > dueDate)
        {
            throw new DomainException("La date d'ouverture ne peut pas être postérieure à l'échéance.");
        }

        OpenDate = openDate;
        DueDate = dueDate;
    }

    public DateOnly OpenDate { get; }

    public DateOnly DueDate { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OpenDate;
        yield return DueDate;
    }
}
