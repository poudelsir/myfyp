using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.Orders;

/// <summary>
/// The single place that knows which <see cref="OrderStatus"/> transitions are legal —
/// so <see cref="OrderService"/>'s six action methods (Create/Accept/Reject/Cancel/
/// MarkReadyForPickup/ConfirmPickup) don't each hand-roll their own precondition
/// check, and no second code path can quietly diverge from it. Same role as
/// <c>VerificationState</c> in the Student Verification module — a small static
/// transition table rather than a stateful workflow engine, since Order's state
/// machine is fixed and small enough not to need one.
/// </summary>
public static class OrderState
{
    private static readonly HashSet<(OrderStatus From, OrderStatus To)> AllowedTransitions = new()
    {
        (OrderStatus.Pending, OrderStatus.Confirmed),
        (OrderStatus.Pending, OrderStatus.Cancelled),
        (OrderStatus.Confirmed, OrderStatus.ReadyForPickup),
        (OrderStatus.Confirmed, OrderStatus.Cancelled),
        (OrderStatus.ReadyForPickup, OrderStatus.Completed),
    };

    /// <summary>
    /// Whether moving directly from <paramref name="from"/> to <paramref name="to"/> is
    /// allowed. Deliberately does NOT allow ReadyForPickup → Cancelled — once a seller
    /// has marked an item ready, the only forward path is the buyer confirming pickup;
    /// this is exactly what the project's Order workflow spec enumerates as the
    /// complete set of allowed transitions, nothing implied beyond it.
    /// </summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        AllowedTransitions.Contains((from, to));
}
