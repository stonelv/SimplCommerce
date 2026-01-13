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
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [Route("api/v1/carts")]
    public class CartPublicApiController : Controller
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Product> _productRepository;

        public CartPublicApiController(
            IRepository<CartItem> cartItemRepository,
            ICartService cartService,
            IWorkContext workContext,
            IRepository<Product> productRepository)
        {
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            _workContext = workContext;
            _productRepository = productRepository;
        }

        [HttpPost("{cartId}/items")]
        public async Task<IActionResult> AddToCart(long cartId, [FromBody] AddToCartRequest request)
        {
            var currentUser = await _workContext.GetCurrentUser();
            
            // 校验商品是否存在且可售
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted && p.IsPublished && p.IsAllowToOrder);
            
            if (product == null)
            {
                return BadRequest(new { error = "商品不存在或不可售" });
            }

            // 校验库存
            if (product.StockTrackingIsEnabled && request.Quantity > product.StockQuantity)
            {
                return BadRequest(new { error = "库存不足" });
            }

            // 合并行项
            var existingCartItem = await _cartItemRepository.Query()
                .FirstOrDefaultAsync(ci => ci.ProductId == request.ProductId && ci.CustomerId == currentUser.Id);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += request.Quantity;
                existingCartItem.LatestUpdatedOn = DateTimeOffset.Now;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CustomerId = currentUser.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    CreatedOn = DateTimeOffset.Now
                };
                _cartItemRepository.Add(cartItem);
            }

            await _cartItemRepository.SaveChangesAsync();

            return Ok(new { message = "商品已成功加入购物车" });
        }
    }

    public class AddToCartRequest
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
    }
}