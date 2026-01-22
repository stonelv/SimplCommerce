using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [Route("api/v1/carts")]
    public class CartPublicApiController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IWorkContext _workContext;

        public CartPublicApiController(
            ICartService cartService,
            IRepository<CartItem> cartItemRepository,
            IRepository<Product> productRepository,
            IWorkContext workContext)
        {
            _cartService = cartService;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _workContext = workContext;
        }

        [HttpPost("{cartId}/items")]
        public async Task<IActionResult> AddItem(long cartId, [FromBody] AddToCartModel model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var product = await _productRepository.Query().FirstOrDefaultAsync(p => p.Id == model.ProductId && !p.IsDeleted && p.IsAllowToOrder);

            if (product == null)
            {
                return BadRequest(new { error = "Product not found or not available for order" });
            }

            if (product.StockTrackingIsEnabled && product.StockQuantity < model.Quantity)
            {
                return BadRequest(new { error = "Insufficient stock" });
            }

            var result = await _cartService.AddToCart(currentUser.Id, model.ProductId, model.Quantity);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new { message = "Item added to cart successfully" });
        }
    }
}