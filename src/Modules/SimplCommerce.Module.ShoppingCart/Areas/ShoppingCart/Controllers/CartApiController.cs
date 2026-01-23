using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [Authorize]
    [Route("api/v1")]
    public class CartApiController : Controller
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;

        public CartApiController(
            IRepository<CartItem> cartItemRepository,
            ICartService cartService,
            IWorkContext workContext)
        {
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            _workContext = workContext;
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
        /// <summary>
        /// 添加商品到购物车
        /// </summary>
        /// <param name="cartId">购物车ID（实际使用用户ID）</param>
        /// <param name="model">添加到购物车的商品信息</param>
        /// <returns>操作结果</returns>
        [HttpPost("carts/{cartId}/items")]
        public async Task<IActionResult> AddToCart(long cartId, [FromBody] AddToCartModel model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            
            // 在这个实现中，cartId实际上是用户ID
            if (cartId != currentUser.Id)
            {
                return Forbid();
            }

            // 校验商品是否可售
            var productRepository = HttpContext.RequestServices.GetService(typeof(IRepository<SimplCommerce.Module.Catalog.Models.Product>)) as IRepository<SimplCommerce.Module.Catalog.Models.Product>;
            var product = await productRepository.Query().FirstOrDefaultAsync(x => x.Id == model.ProductId);
            
            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            if (!product.IsAllowToOrder || !product.IsPublished || product.IsDeleted)
            {
                return BadRequest(new { error = "Product is not available for order" });
            }

            // 校验库存
            if (product.StockTrackingIsEnabled && model.Quantity > product.StockQuantity)
            {
                return BadRequest(new { error = "Not enough stock available" });
            }

            // 添加到购物车
            var result = await _cartService.AddToCart(currentUser.Id, model.ProductId, model.Quantity);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new { message = "Product added to cart successfully" });
        }
    }
}
