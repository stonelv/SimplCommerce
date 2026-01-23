using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Payments.Models;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;

namespace SimplCommerce.Module.Payments.Areas.Payments.Controllers
{
    [Area("Payments")]
    [Authorize(Roles = "admin")]
    [Route("api/payments")]
    public class PaymentApiController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IRepository<Order> _orderRepository;

        public PaymentApiController(IRepository<Payment> paymentRepository, ICurrencyService currencyService, IRepository<Order> orderRepository)
        {
            _paymentRepository = paymentRepository;
            _currencyService = currencyService;
            _orderRepository = orderRepository;
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

        [HttpPost("/api/v1/payments/webhooks/{provider}")]
        [AllowAnonymous]
        public async Task<IActionResult> PostPaymentWebhook(string provider, [FromBody] dynamic requestBody)
        {
            try
            {
                // 1. 签名校验
                if (!await VerifySignature(provider, requestBody, Request.Headers))
                {
                    return Unauthorized(new { error = "签名验证失败" });
                }

                // 2. 解析支付回调参数
                string transactionId = requestBody.transaction_id;
                long orderId = requestBody.order_id;
                string paymentStatus = requestBody.status;
                decimal amount = requestBody.amount;

                // 3. 幂等性检查
                var existingPayment = await _paymentRepository.Query()
                    .FirstOrDefaultAsync(p => p.GatewayTransactionId == transactionId && p.PaymentMethod == provider);

                if (existingPayment != null)
                {
                    // 已经处理过该支付回调，直接返回成功
                    return Ok(new { message = "支付回调已处理" });
                }

                // 4. 获取订单
                var order = await _orderRepository.Query().FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    return NotFound(new { error = "订单不存在" });
                }

                // 5. 创建支付记录
                var payment = new Payment
                {
                    OrderId = orderId,
                    Amount = amount,
                    PaymentFee = 0, // 根据实际支付网关的回调数据设置
                    PaymentMethod = provider,
                    GatewayTransactionId = transactionId,
                    Status = MapPaymentStatus(paymentStatus),
                    CreatedOn = DateTimeOffset.Now,
                    LatestUpdatedOn = DateTimeOffset.Now
                };

                // 6. 更新订单状态
                if (MapPaymentStatus(paymentStatus) == PaymentStatus.Succeeded)
                {
                    order.OrderStatus = OrderStatus.PaymentReceived;
                    await _orderRepository.SaveChangesAsync();
                }
                else if (MapPaymentStatus(paymentStatus) == PaymentStatus.Failed)
                {
                    order.OrderStatus = OrderStatus.PaymentFailed;
                    await _orderRepository.SaveChangesAsync();
                }

                // 7. 保存支付记录
                _paymentRepository.Add(payment);
                await _paymentRepository.SaveChangesAsync();

                return Ok(new { message = "支付状态更新成功" });
            }
            catch (Exception ex)
            {
                // 记录错误日志
                return StatusCode(500, new { error = "支付回调处理失败" });
            }
        }

        /// <summary>
        /// 验证支付签名
        /// </summary>
        private async Task<bool> VerifySignature(string provider, dynamic requestBody, Microsoft.AspNetCore.Http.IHeaderDictionary headers)
        {
            // 这里需要根据不同的支付网关实现具体的签名验证逻辑
            // 例如：
            // - 从headers中获取签名
            // - 使用网关提供的密钥重新计算签名
            // - 比较两个签名是否一致

            // 为了简化，这里返回true，实际项目中需要实现具体的签名验证
            return true;
        }

        /// <summary>
        /// 将支付网关的状态映射到系统的支付状态
        /// </summary>
        private PaymentStatus MapPaymentStatus(string paymentStatus)
        {
            // 根据不同支付网关的状态值映射到系统的PaymentStatus枚举
            switch (paymentStatus.ToLower())
            {
                case "completed":
                case "success":
                    return PaymentStatus.Succeeded;
                case "failed":
                case "cancelled":
                case "refunded":
                case "pending":
                    return PaymentStatus.Failed;
                default:
                    return PaymentStatus.Failed;
            }
        }
    }
}
