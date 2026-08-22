namespace SajhaSikshya.Services.Interfaces.Payments;

/// <summary>
/// A simulated eSewa/Khalti checkout — no real gateway is wired up (there are no
/// merchant credentials to integrate against), so this stands in for one: it validates
/// the order the same way a real callback handler would (right buyer, right order,
/// still awaiting payment) and then simply records the outcome the buyer chose on the
/// simulated checkout screen. Swapping in a real gateway later means replacing this
/// implementation behind the same interface — callers (PaymentsController) don't change.
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>Records a successful simulated payment. Fails unless the order belongs to <paramref name="buyerId"/>, uses an online <c>PaymentMethod</c>, and its <c>PaymentStatus</c> is still Pending or previously Failed.</summary>
    Task<ServiceResult> SimulateSuccessAsync(int orderId, string buyerId);

    /// <summary>Records a failed simulated payment, so the buyer can retry or switch to Cash on Pickup. Same guards as <see cref="SimulateSuccessAsync"/>.</summary>
    Task<ServiceResult> SimulateFailureAsync(int orderId, string buyerId);
}
