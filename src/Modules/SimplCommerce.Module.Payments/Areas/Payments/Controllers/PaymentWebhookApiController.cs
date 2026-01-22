using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;

namespace SimplCommerce.Module.Payments.Areas.Payments.Controllers
{
    [Area("Payments")]
    [Route("api/v1/payments/webhooks")]
    public class PaymentWebhookApiController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IRepository<Order> _orderRepository;

        public PaymentWebhookApiController(
            IOrderService orderService,
            IRepository<Order> orderRepository)
        {
            _orderService = orderService;
            _orderRepository = orderRepository;
        }

        [HttpPost("{provider}")]
        public async Task<IActionResult> Webhook(string provider, [FromBody] PaymentWebhookRequest request)
        {
            if (!ValidateSignature(provider, request))
            {
                return BadRequest(new { error = "Invalid signature" });
            }

            if (await IsDuplicateRequest(request.IdempotencyKey))
            {
                return Ok(new { message = "Request already processed" });
            }

            var order = await _orderRepository.Query().FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            using (var transaction = _orderRepository.BeginTransaction())
            {
                try
                {
                    switch (request.Status.ToLower())
                    {
                        case "paid":
                            order.OrderStatus = OrderStatus.PaymentReceived;
                            break;
                        case "failed":
                            order.OrderStatus = OrderStatus.PaymentFailed;
                            break;
                        case "refunded":
                            order.OrderStatus = OrderStatus.Refunded;
                            break;
                    }

                    _orderRepository.SaveChanges();
                    await SaveIdempotencyKey(request.IdempotencyKey);
                    transaction.Commit();

                    return Ok(new { message = "Webhook processed successfully" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new { error = "Failed to process webhook: " + ex.Message });
                }
            }
        }

        private bool ValidateSignature(string provider, PaymentWebhookRequest request)
        {
            return true;
        }

        private async Task<bool> IsDuplicateRequest(string idempotencyKey)
        {
            return false;
        }

        private async Task SaveIdempotencyKey(string idempotencyKey)
        {
        }
    }

    public class PaymentWebhookRequest
    {
        public long OrderId { get; set; }
        public string Status { get; set; }
        public string IdempotencyKey { get; set; }
    }


}