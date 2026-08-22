using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Extensions;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Payments;
using SajhaSikshya.Services.Notifications;

namespace SajhaSikshya.Services.Payments;

/// <inheritdoc cref="IPaymentGatewayService"/>
public class SimulatedPaymentGatewayService : IPaymentGatewayService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SimulatedPaymentGatewayService> _logger;

    public SimulatedPaymentGatewayService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<SimulatedPaymentGatewayService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ServiceResult> SimulateSuccessAsync(int orderId, string buyerId)
    {
        var order = await LoadPayableOrderAsync(orderId, buyerId);
        if (order is null)
        {
            return ServiceResult.Failure("This order is not awaiting an online payment.");
        }

        order.PaymentStatus = PaymentStatus.Completed;
        order.PaymentTransactionId = $"SIM-{order.PaymentMethod.ToString().ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
        order.PaymentCompletedAtUtc = DateTime.UtcNow;

        _unitOfWork.Repository<Order>().Update(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Simulated payment completed for order {OrderId} via {PaymentMethod} (transaction {TransactionId}).",
            order.Id, order.PaymentMethod, order.PaymentTransactionId);

        var listing = await _unitOfWork.Repository<Listing>().GetByIdAsync(order.ListingId);
        var (title, message) = NotificationTemplates.PaymentReceived(listing?.Title ?? "your listing", order.PaymentMethod.GetDisplayName());
        await _notificationService.CreateAsync(order.SellerId, NotificationType.Order, title, message, $"/Student/Orders/Details/{order.Id}", createdBy: buyerId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SimulateFailureAsync(int orderId, string buyerId)
    {
        var order = await LoadPayableOrderAsync(orderId, buyerId);
        if (order is null)
        {
            return ServiceResult.Failure("This order is not awaiting an online payment.");
        }

        order.PaymentStatus = PaymentStatus.Failed;
        _unitOfWork.Repository<Order>().Update(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Simulated payment failed for order {OrderId} via {PaymentMethod}.", order.Id, order.PaymentMethod);

        return ServiceResult.Success();
    }

    /// <summary>Loads the order only if it's payable by this simulated flow — belongs to <paramref name="buyerId"/>, uses an online payment method, and hasn't already been paid.</summary>
    private async Task<Order?> LoadPayableOrderAsync(int orderId, string buyerId)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId);
        if (order is null || order.BuyerId != buyerId)
        {
            return null;
        }

        if (order.PaymentMethod is not (PaymentMethod.ESewa or PaymentMethod.Khalti))
        {
            return null;
        }

        if (order.PaymentStatus is not (PaymentStatus.Pending or PaymentStatus.Failed))
        {
            return null;
        }

        return order;
    }
}
