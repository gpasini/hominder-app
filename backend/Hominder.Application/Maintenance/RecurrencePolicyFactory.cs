using Hominder.Domain.Common;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Application.Maintenance;

public static class RecurrencePolicyFactory
{
    public static RecurrencePolicy Create(RecurrencePolicyInput input) => input.Kind switch
    {
        RecurrenceKind.Interval => new IntervalPolicy(
            Required(input.IntervalAmount, "L'intervalle est obligatoire."),
            Required(input.IntervalUnit, "L'unité d'intervalle est obligatoire."),
            Required(input.StartReference, "La date de départ est obligatoire.")),
        RecurrenceKind.MonthWindow => new MonthWindowPolicy(
            Required(input.StartMonth, "Le mois de début est obligatoire."),
            Required(input.EndMonth, "Le mois de fin est obligatoire.")),
        RecurrenceKind.FixedDate => new FixedDatePolicy(
            Required(input.DueDate, "L'échéance est obligatoire.")),
        RecurrenceKind.OneOff => new OneOffPolicy(
            Required(input.DueDate, "L'échéance est obligatoire.")),
        _ => throw new DomainException("Type de récurrence inconnu."),
    };

    private static T Required<T>(T? value, string message)
        where T : struct =>
        value ?? throw new DomainException(message);
}
