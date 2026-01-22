using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.ShoppingCart.Models;
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
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Order> _orderRepository;

        public OrderPublicApiController(
            IOrderService orderService,
            ICartService cartService,
            IWorkContext workContext,
            IRepository<CartItem> cartItemRepository,
            IRepository<Product> productRepository,
            IRepository<Order> orderRepository)
        {
            _orderService = orderService;
            _cartService = cartService;
            _workContext = workContext;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var currentUser = await _workContext.GetCurrentUser();
            
            // 幂等校验
            if (!string.IsNullOrEmpty(request.RequestId))
            {
                // 检查是否已处理过该请求
                var existingOrder = await _orderRepository.Query()
                    .FirstOrDefaultAsync(o => o.CustomerId == currentUser.Id);
                if (existingOrder != null)
                {
                    return Ok(new { orderId = existingOrder.Id });
                }
            }

            // 获取购物车商品
            var cartItems = await _cartItemRepository.Query()
                .Include(ci => ci.Product)
                .Where(ci => ci.CustomerId == currentUser.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return BadRequest(new { error = "购物车为空" });
            }

            // 校验库存
            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product.StockTrackingIsEnabled && cartItem.Quantity > cartItem.Product.StockQuantity)
                {
                    return BadRequest(new { error = $"商品 {cartItem.Product.Name} 库存不足" });
                }
            }

            // 创建订单
            var order = new Order
            {
                CustomerId = currentUser.Id,
                CreatedById = currentUser.Id,
                CreatedOn = DateTimeOffset.Now,
                LatestUpdatedOn = DateTimeOffset.Now,
                LatestUpdatedById = currentUser.Id,
                PaymentMethod = request.PaymentMethod,
                OrderStatus = OrderStatus.New
            };

            // 添加订单项
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductPrice = cartItem.Product.Price,
                    Quantity = cartItem.Quantity
                };

                order.OrderItems.Add(orderItem);

                // 扣减库存
                cartItem.Product.StockQuantity -= cartItem.Quantity;
            }

            // 计算订单总额
            order.SubTotal = order.OrderItems.Sum(oi => oi.ProductPrice * oi.Quantity);
            order.OrderTotal = order.SubTotal;

            // 保存订单
            _orderRepository.Add(order);
            await _orderRepository.SaveChangesAsync();

            // 清空购物车
            foreach (var cartItem in cartItems)
            {
                _cartItemRepository.Remove(cartItem);
            }
            await _cartItemRepository.SaveChangesAsync();

            return Ok(new { orderId = order.Id });
        }
    }

    public class CreateOrderRequest
    {
        public string RequestId { get; set; }
        public string PaymentMethod { get; set; }
    }
}