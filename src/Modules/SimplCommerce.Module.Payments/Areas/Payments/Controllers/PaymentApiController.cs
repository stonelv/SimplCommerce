using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.Payments.Areas.Payments.ViewModels;
using SimplCommerce.Module.Payments.Models;

namespace SimplCommerce.Module.Payments.Areas.Payments.Controllers
{
    [Area("Payments")]
    [AllowAnonymous]
    [Route("api/v1/payments")]
    public class PaymentApiController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;
    private readonly ICurrencyService _currencyService;
    private readonly IRepository<Order> _orderRepository;
    private readonly IOrderService _orderService;

    public PaymentApiController(IRepository<Payment> paymentRepository, ICurrencyService currencyService, IRepository<Order> orderRepository, IOrderService orderService)
    {
        _paymentRepository = paymentRepository;
        _currencyService = currencyService;
        _orderRepository = orderRepository;
        _orderService = orderService;
    }

        [HttpGet("/api/orders/{orderId}/payments")]
        public async Task<IActionResult> GetByOrder(long orderId)
        {
            var payments = await _paymentRepository.Query()
                .Where(x => x.OrderId == orderId)
                .Select(x => new
                {
                    x.Id,
                    x.Amount,
                    AmountString = _currencyService.FormatCurrency(x.Amount),
                    x.PaymentFee,
                    PaymentFeeString = _currencyService.FormatCurrency(x.PaymentFee),
                    x.OrderId,
                    x.GatewayTransactionId,
                    Status = x.Status.ToString(),
                    x.CreatedOn
                }).ToListAsync();

            return Ok(payments);
        }

        [HttpPost("webhooks/{provider}")]
        public async Task<IActionResult> Webhook(string provider, [FromQuery] string transactionId)
        {
            // Validate required parameters
            if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(transactionId))
            {
                return BadRequest(new { error = "Provider and transactionId are required" });
            }

            // Basic signature verification would go here
            // In a real implementation, you would verify the signature from the payment provider
            // For example: VerifySignature(Request.Headers["X-Signature"], Request.Body, provider);
            
            // Check for existing payment with same transaction ID (idempotency check)
            var existingPayment = await _paymentRepository.Query()
                .FirstOrDefaultAsync(p => p.GatewayTransactionId == transactionId);
            
            if (existingPayment != null)
            {
                // Return success for idempotent requests
                return Ok(new { message = "Payment already processed", paymentStatus = existingPayment.Status.ToString() });
            }

            // Get order and payment details from request body
            // For simplicity, we'll assume the request body contains the necessary information
            // In a real implementation, you would parse the specific format from each payment provider
            PaymentWebhookModel paymentInfo;
            using (var reader = new StreamReader(Request.Body))
            {
                var requestBody = await reader.ReadToEndAsync();
                paymentInfo = JsonSerializer.Deserialize<PaymentWebhookModel>(requestBody);
            }
            
            if (paymentInfo == null || paymentInfo.OrderId <= 0)
            {
                return BadRequest(new { error = "Invalid payment information" });
            }

            // Get order
            var order = await _orderRepository.Query().FirstOrDefaultAsync(o => o.Id == paymentInfo.OrderId);
            
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            // Create payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = paymentInfo.Amount,
                PaymentFee = paymentInfo.PaymentFee,
                PaymentMethod = provider,
                GatewayTransactionId = transactionId,
                Status = paymentInfo.Status,
                FailureMessage = paymentInfo.FailureMessage
            };

            // Update order status based on payment status
            if (paymentInfo.Status == PaymentStatus.Succeeded)
            {
                // Update order status to Paid in a transaction-safe manner
                order.OrderStatus = OrderStatus.Paid;
                order.LatestUpdatedOn = DateTimeOffset.Now;
            }
            else if (paymentInfo.Status == PaymentStatus.Failed)
            {
                // Update order status to PaymentFailed
                order.OrderStatus = OrderStatus.PaymentFailed;
                order.LatestUpdatedOn = DateTimeOffset.Now;
            }

            // Save changes in a transaction
            await _paymentRepository.AddAsync(payment);
            await _orderRepository.SaveChangesAsync();

            return Ok(new { message = "Payment processed successfully", paymentStatus = payment.Status.ToString() });
        }
    }
}
