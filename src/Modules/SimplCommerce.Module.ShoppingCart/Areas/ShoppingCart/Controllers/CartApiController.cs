using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [Route("api/v1/carts")]
    [Authorize]
    public class CartApiController : Controller
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Product> _productRepository;

        public CartApiController(
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

        //[HttpGet("api/customers/{customerId}/cart")]
        //public async Task<IActionResult> List(long customerId)
        //{
        //    var currentUser = await _workContext.GetCurrentUser();
        //    var cart = await _cartService.GetActiveCartDetails(customerId, currentUser.Id);

        //    return Json(cart);
        //}

        //[HttpPost("api/customers/{customerId}/add-cart-item")]
        //public async Task<IActionResult> AddToCart(long customerId, [FromBody] AddToCartModel model)
        //{
        //    var currentUser = await _workContext.GetCurrentUser();
        //    await _cartService.AddToCart(customerId, currentUser.Id, model.ProductId, model.Quantity);

        //    return Accepted();
        //}

        //[HttpPut("api/carts/items/{itemId}")]
        //public async Task<IActionResult> UpdateQuantity(long itemId, [FromBody] CartQuantityUpdate model)
        //{
        //    var currentUser = await _workContext.GetCurrentUser();
        //    var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.Cart.CreatedById == currentUser.Id);
        //    if (cartItem == null)
        //    {
        //        return NotFound();
        //    }

        //    cartItem.Quantity = model.Quantity;
        //    _cartItemRepository.SaveChanges();

        //    return Accepted();
        //}

        //[HttpPost("api/carts/{cartId}/apply-coupon")]
        //public async Task<ActionResult> ApplyCoupon(long cartId, [FromBody] ApplyCouponForm model)
        //{
        //    var currentUser = await _workContext.GetCurrentUser();
        //    var cart = await _cartService.Query().FirstOrDefaultAsync(x => x.Id == cartId && x.CreatedById == currentUser.Id);
        //    if (cart == null)
        //    {
        //        return NotFound();
        //    }

        //    var validationResult = await _cartService.ApplyCoupon(cart.Id, model.CouponCode);
        //    if (validationResult.Succeeded)
        //    {
        //        var cartVm = await _cartService.GetActiveCartDetails(currentUser.Id);
        //        return Json(cartVm);
        //    }

        //    return Json(validationResult);
        //}

        //[HttpDelete("api/carts/items/{itemId}")]
        //public async Task<IActionResult> Remove(long itemId)
        //{
        //    var currentUser = await _workContext.GetCurrentUser();
        //    var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.Cart.CreatedById == currentUser.Id);
        //    if (cartItem == null)
        //    {
        //        return NotFound();
        //    }

        //    _cartItemRepository.Remove(cartItem);
        //    _cartItemRepository.SaveChanges();

        //    return NoContent();
        //}

        [HttpPost("{cartId}/items")]
        public async Task<IActionResult> AddCartItem(Guid cartId, [FromBody] AddToCartModel model)
        {
            var currentUser = await _workContext.GetCurrentUser();

            // 验证商品是否存在且可售
            var product = await _productRepository.Query().FirstOrDefaultAsync(p => p.Id == model.ProductId);
            if (product == null)
            {
                return NotFound(new { error = "商品不存在" });
            }

            if (!product.IsAllowToOrder || !product.IsPublished || product.IsDeleted)
            {
                return BadRequest(new { error = "商品不可购买" });
            }

            // 验证库存
            if (product.StockTrackingIsEnabled && product.StockQuantity < model.Quantity)
            {
                return BadRequest(new { error = "库存不足", availableStock = product.StockQuantity });
            }

            // 查找购物车项
            var cartItem = await _cartItemRepository.Query()
                .FirstOrDefaultAsync(ci => ci.CustomerId == currentUser.Id && ci.ProductId == model.ProductId);

            if (cartItem == null)
            {
                // 添加新购物车项
                cartItem = new CartItem
                {
                    CustomerId = currentUser.Id,
                    ProductId = model.ProductId,
                    Quantity = model.Quantity
                };
                _cartItemRepository.Add(cartItem);
            }
            else
            {
                // 合并行项，更新数量
                var newQuantity = cartItem.Quantity + model.Quantity;
                
                // 验证合并后的数量是否超过库存
                if (product.StockTrackingIsEnabled && newQuantity > product.StockQuantity)
                {
                    return BadRequest(new { error = "库存不足", availableStock = product.StockQuantity });
                }
                
                cartItem.Quantity = newQuantity;
                cartItem.LatestUpdatedOn = DateTimeOffset.Now;
            }

            await _cartItemRepository.SaveChangesAsync();

            // 返回更新后的购物车详情
            var cart = await _cartService.GetCartDetails(currentUser.Id);
            return Ok(cart);
        }
    }
}
