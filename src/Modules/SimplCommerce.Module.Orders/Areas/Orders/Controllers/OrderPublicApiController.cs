using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Route("api/v1/orders")]
    public class OrderPublicApiController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;

        public OrderPublicApiController(
            IOrderService orderService,
            ICartService cartService,
            IWorkContext workContext)
        {
            _orderService = orderService;
            _cartService = cartService;
            _workContext = workContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cartVm = await _cartService.GetCartDetails(currentUser.Id);

            if (cartVm == null || cartVm.Items == null || cartVm.Items.Count == 0)
            {
                return BadRequest(new { error = "Cart is empty or not found" });
            }

            // Note: This implementation requires checkoutId, which is not directly available from cart service
            // You would need to implement a way to get checkoutId from user session or cart
            var checkoutId = Guid.NewGuid(); // This is a placeholder, need to implement proper logic
            var orderResult = await _orderService.CreateOrder(checkoutId, model.PaymentMethod, model.PaymentMethodAdditionalFee);

            if (!orderResult.Success)
            {
                return BadRequest(new { error = orderResult.Error });
            }

            return Created($"/api/v1/orders/{orderResult.Value.Id}", new { id = orderResult.Value.Id });
        }
    }

    public class CreateOrderRequest
    {
        public long CartId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal PaymentMethodAdditionalFee { get; set; }
    }
}