using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Helpers;
using SimplCommerce.Infrastructure.Web.SmartTable;
using SimplCommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using SimplCommerce.Module.Checkouts.Services;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Orders.Areas.Orders.ViewModels;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.ShoppingCart.Services;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;

namespace SimplCommerce.Module.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Authorize(Roles = "admin, vendor")]
    [Route("api/orders")]
    public class OrderApiController : Controller
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderService _orderService;
        private readonly ICheckoutService _checkoutService;
        private readonly ICartService _cartService;

        public OrderApiController(IRepository<Order> orderRepository, IWorkContext workContext, IMediator mediator, 
            ICurrencyService currencyService, IOrderService orderService, ICheckoutService checkoutService, ICartService cartService)
        {
            _orderRepository = orderRepository;
            _workContext = workContext;
            _mediator = mediator;
            _currencyService = currencyService;
            _orderService = orderService;
            _checkoutService = checkoutService;
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<ActionResult> Get(int status, int numRecords)
        {
            var orderStatus = (OrderStatus)status;
            if ((numRecords <= 0) || (numRecords > 100))
            {
                numRecords = 5;
            }

            var query = _orderRepository.Query();
            if (orderStatus != 0)
            {
                query = query.Where(x => x.OrderStatus == orderStatus);
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }

            var model = query.OrderByDescending(x => x.CreatedOn)
                .Take(numRecords)
                .Select(x => new
                {
                    x.Id,
                    CustomerName = x.Customer.FullName,
                    x.OrderTotal,
                    OrderTotalString = _currencyService.FormatCurrency(x.OrderTotal),
                    OrderStatus = x.OrderStatus.ToString(),
                    x.CreatedOn
                });

            return Json(model);
        }

        [HttpPost("grid")]
        public async Task<ActionResult> List([FromBody] SmartTableParam param)
        {
            var query = _orderRepository
                .Query();

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (OrderStatus)search.Status;
                    query = query.Where(x => x.OrderStatus == status);
                }

                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Customer.FullName.Contains(customerName));
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            var orders = query.ToSmartTableResult(
                param,
                order => new
                {
                    order.Id,
                    CustomerName = order.Customer.FullName,
                    order.OrderTotal,
                    OrderTotalString = _currencyService.FormatCurrency(order.OrderTotal),
                    OrderStatus = order.OrderStatus.ToString(),
                    order.CreatedOn
                });

            return Json(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var order = _orderRepository
                .Query()
                .Include(x => x.ShippingAddress).ThenInclude(x => x.District)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.Country)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ThumbnailImage)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .Include(x => x.Customer)
                .FirstOrDefault(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            var model = new OrderDetailVm(_currencyService)
            {
                Id = order.Id,
                IsMasterOrder = order.IsMasterOrder,
                CreatedOn = order.CreatedOn,
                OrderStatus = (int)order.OrderStatus,
                OrderStatusString = order.OrderStatus.ToString(),
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                ShippingMethod = order.ShippingMethod,
                PaymentMethod = order.PaymentMethod,
                PaymentFeeAmount = order.PaymentFeeAmount,
                Subtotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                SubTotalWithDiscount = order.SubTotalWithDiscount,
                TaxAmount = order.TaxAmount,
                ShippingAmount = order.ShippingFeeAmount,
                OrderTotal = order.OrderTotal,
                OrderNote = order.OrderNote,
                ShippingAddress = new ShippingAddressVm
                {
                    AddressLine1 = order.ShippingAddress.AddressLine1,
                    CityName = order.ShippingAddress.City,
                    ZipCode = order.ShippingAddress.ZipCode,
                    ContactName = order.ShippingAddress.ContactName,
                    DistrictName = order.ShippingAddress.District?.Name,
                    StateOrProvinceName = order.ShippingAddress.StateOrProvince.Name,
                    Phone = order.ShippingAddress.Phone
                },
                OrderItems = order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.Name,
                    ProductPrice = x.ProductPrice,
                    Quantity = x.Quantity,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TaxPercent = x.TaxPercent,
                    VariationOptions = OrderItemVm.GetVariationOption(x.Product)
                }).ToList()
            };

            if (order.IsMasterOrder)
            {
                model.SubOrderIds = _orderRepository.Query().Where(x => x.ParentId == order.Id).Select(x => x.Id).ToList();
            }

            await _mediator.Publish(new OrderDetailGot { OrderDetailVm = model });

            return Json(model);
        }

        [HttpPost("change-order-status/{id}")]
        public async Task<IActionResult> ChangeStatus(long id, [FromBody] OrderStatusForm model)
        {
            var order = _orderRepository.Query().FirstOrDefault(x => x.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            if (Enum.IsDefined(typeof(OrderStatus), model.StatusId))
            {
                var oldStatus = order.OrderStatus;
                order.OrderStatus = (OrderStatus)model.StatusId;
                await _orderRepository.SaveChangesAsync();

                var orderStatusChanged = new OrderChanged
                {
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = order.OrderStatus,
                    Order = order,
                    UserId = currentUser.Id,
                    Note = model.Note
                };

                await _mediator.Publish(orderStatusChanged);
                return Accepted();
            }

            return BadRequest(new { Error = "unsupported order status" });
        }

        [HttpGet("order-status")]
        public IActionResult GetOrderStatus()
        {
            var model = EnumHelper.ToDictionary(typeof(OrderStatus)).Select(x => new { Id = x.Key, Name = x.Value });
            return Json(model);
        }

        [HttpPost("export")]
        public async Task<ActionResult<OrderExportVm>> Export([FromBody] SmartTableParam param)
        {
            var query = _orderRepository.Query();

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (OrderStatus)search.Status;
                    query = query.Where(x => x.OrderStatus == status);
                }

                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Customer.FullName.Contains(customerName));
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            var orders = await query
                .Select(x => new OrderExportVm
                {
                    Id = x.Id,
                    OrderStatus = (int)x.OrderStatus,
                    IsMasterOrder = x.IsMasterOrder,
                    DiscountAmount = x.DiscountAmount,
                    CreatedOn = x.CreatedOn,
                    OrderStatusString = x.OrderStatus.GetDisplayName(),
                    PaymentFeeAmount = x.PaymentFeeAmount,
                    OrderTotal = x.OrderTotal,
                    Subtotal = x.SubTotal,
                    SubtotalWithDiscount = x.SubTotalWithDiscount,
                    PaymentMethod = x.PaymentMethod,
                    ShippingAmount = x.ShippingFeeAmount,
                    ShippingMethod = x.ShippingMethod,
                    TaxAmount = x.TaxAmount,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.FullName,
                    CustomerEmail = x.Customer.Email,
                    LatestUpdatedOn = x.LatestUpdatedOn,
                    Coupon = x.CouponCode,
                    Items = x.OrderItems.Count(),
                    BillingAddressId = x.BillingAddressId,
                    BillingAddressAddressLine1 = x.BillingAddress.AddressLine1,
                    BillingAddressAddressLine2 = x.BillingAddress.AddressLine2,
                    BillingAddressContactName = x.BillingAddress.ContactName,
                    BillingAddressCountryName = x.BillingAddress.Country.Name,
                    BillingAddressDistrictName = x.BillingAddress.District.Name,
                    BillingAddressZipCode = x.BillingAddress.ZipCode,
                    BillingAddressPhone = x.BillingAddress.Phone,
                    BillingAddressStateOrProvinceName = x.BillingAddress.StateOrProvince.Name,
                    ShippingAddressAddressLine1 = x.ShippingAddress.AddressLine1,
                    ShippingAddressAddressLine2 = x.ShippingAddress.AddressLine2,
                    ShippingAddressId = x.ShippingAddressId,
                    ShippingAddressContactName = x.ShippingAddress.ContactName,
                    ShippingAddressCountryName = x.ShippingAddress.Country.Name,
                    ShippingAddressDistrictName = x.ShippingAddress.District.Name,
                    ShippingAddressPhone = x.ShippingAddress.Phone,
                    ShippingAddressStateOrProvinceName = x.ShippingAddress.StateOrProvince.Name,
                    ShippingAddressZipCode = x.ShippingAddress.ZipCode
                })
                .ToListAsync();

            foreach(var order in orders)
            {
                order.SubtotalString = _currencyService.FormatCurrency(order.Subtotal);
                order.DiscountAmountString = _currencyService.FormatCurrency(order.DiscountAmount);
                order.SubtotalWithDiscountString = _currencyService.FormatCurrency(order.SubtotalWithDiscount);
                order.TaxAmountString = _currencyService.FormatCurrency(order.TaxAmount);
                order.ShippingAmountString = _currencyService.FormatCurrency(order.ShippingAmount);
                order.PaymentFeeAmountString = _currencyService.FormatCurrency(order.PaymentFeeAmount);
                order.OrderTotalString = _currencyService.FormatCurrency(order.OrderTotal);
            }

            var csvString = CsvConverter.ExportCsv(orders);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "orders-export.csv");
        }

        [HttpPost]
        [Route("api/v1/orders")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrderFromCart([FromBody] CreateOrderRequest request)
        {
            // 幂等性检查
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                // 这里可以实现幂等性检查逻辑，例如检查是否已经存在相同IdempotencyKey的订单
                // 为了简化，这里省略了具体实现
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // 获取购物车详情
            var cart = await _cartService.GetCartDetails(currentUser.Id);
            if (cart == null || !cart.Items.Any())
            {
                return BadRequest(new { error = "购物车为空" });
            }

            // 创建结账信息
            var cartItemsToCheckout = cart.Items.Select(x => new CartItemToCheckoutVm
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity
            }).ToList();

            var checkout = await _checkoutService.Create(currentUser.Id, currentUser.Id, cartItemsToCheckout, cart.CouponCode);

            // 更新税收和运费
            var taxAndShippingRequest = new TaxAndShippingPriceRequestVm
            {
                ExistingShippingAddressId = request.ShippingAddressId,
                SelectedShippingMethodName = request.ShippingMethod
            };

            if (request.ShippingAddressId == 0 && request.NewShippingAddress != null)
            {
                taxAndShippingRequest.NewShippingAddress = request.NewShippingAddress;
            }

            await _checkoutService.UpdateTaxAndShippingPrices(checkout.Id, taxAndShippingRequest);

            // 创建订单
            var orderResult = await _orderService.CreateOrder(checkout.Id, request.PaymentMethod, request.PaymentFeeAmount);
            if (!orderResult.Success)
            {
                return BadRequest(new { error = orderResult.Error });
            }

            // 返回订单信息
            return CreatedAtAction(nameof(Get), new { id = orderResult.Value.Id }, orderResult.Value);
        }

        [HttpPost("lines-export")]
        public async Task<ActionResult<OrderLineExportVm>> OrderLinesExport([FromBody] SmartTableParam param, [FromServices] IRepository<OrderItem> orderItemRepository)
        {
            var query = orderItemRepository.Query();

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.Order.VendorId == currentUser.VendorId);
            }

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (OrderStatus)search.Status;
                    query = query.Where(x => x.Order.OrderStatus == status);
                }

                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Order.Customer.FullName.Contains(customerName));
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.Order.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.Order.CreatedOn >= after);
                    }
                }
            }

            var orderItems = await query
                            .Select(x => new OrderLineExportVm()
                            {
                                Id = x.Id,
                                OrderStatus = (int)x.Order.OrderStatus,
                                IsMasterOrder = x.Order.IsMasterOrder,
                                DiscountAmount = x.Order.DiscountAmount,
                                CreatedOn = x.Order.CreatedOn,
                                OrderStatusString = x.Order.OrderStatus.ToString(),
                                PaymentFeeAmount = x.Order.PaymentFeeAmount,
                                OrderTotal = x.Order.OrderTotal,
                                Subtotal = x.Order.SubTotal,
                                SubtotalWithDiscount = x.Order.SubTotalWithDiscount,
                                PaymentMethod = x.Order.PaymentMethod,
                                ShippingAmount = x.Order.ShippingFeeAmount,
                                ShippingMethod = x.Order.ShippingMethod,
                                TaxAmount = x.Order.TaxAmount,
                                CustomerId = x.Order.CustomerId,
                                CustomerName = x.Order.Customer.FullName,
                                CustomerEmail = x.Order.Customer.Email,
                                LatestUpdatedOn = x.Order.LatestUpdatedOn,
                                Coupon = x.Order.CouponCode,
                                Items = x.Order.OrderItems.Count(),
                                BillingAddressId = x.Order.BillingAddressId,
                                BillingAddressAddressLine1 = x.Order.BillingAddress.AddressLine1,
                                BillingAddressAddressLine2 = x.Order.BillingAddress.AddressLine2,
                                BillingAddressContactName = x.Order.BillingAddress.ContactName,
                                BillingAddressCountryName = x.Order.BillingAddress.Country.Name,
                                BillingAddressDistrictName = x.Order.BillingAddress.District.Name,
                                BillingAddressZipCode = x.Order.BillingAddress.ZipCode,
                                BillingAddressPhone = x.Order.BillingAddress.Phone,
                                BillingAddressStateOrProvinceName = x.Order.BillingAddress.StateOrProvince.Name,
                                ShippingAddressAddressLine1 = x.Order.ShippingAddress.AddressLine1,
                                ShippingAddressAddressLine2 = x.Order.ShippingAddress.AddressLine2,
                                ShippingAddressId = x.Order.ShippingAddressId,
                                ShippingAddressContactName = x.Order.ShippingAddress.ContactName,
                                ShippingAddressCountryName = x.Order.ShippingAddress.Country.Name,
                                ShippingAddressDistrictName = x.Order.ShippingAddress.District.Name,
                                ShippingAddressPhone = x.Order.ShippingAddress.Phone,
                                ShippingAddressStateOrProvinceName = x.Order.ShippingAddress.StateOrProvince.Name,
                                ShippingAddressZipCode = x.Order.ShippingAddress.ZipCode,
                                OrderLineDiscountAmount = x.DiscountAmount,
                                OrderLineQuantity = x.Quantity,
                                OrderLineTaxAmount = x.TaxAmount,
                                OrderLineTaxPercent = x.TaxPercent,
                                OrderLineId = x.Id,
                                ProductId = x.ProductId,
                                ProductName = x.Product.Name,
                                ProductPrice = x.ProductPrice
                            })
                            .ToListAsync();

            foreach (var item in orderItems)
            {
                item.SubtotalString = _currencyService.FormatCurrency(item.Subtotal);
                item.DiscountAmountString = _currencyService.FormatCurrency(item.DiscountAmount);
                item.SubtotalWithDiscountString = _currencyService.FormatCurrency(item.SubtotalWithDiscount);
                item.TaxAmountString = _currencyService.FormatCurrency(item.TaxAmount);
                item.ShippingAmountString = _currencyService.FormatCurrency(item.ShippingAmount);
                item.PaymentFeeAmountString = _currencyService.FormatCurrency(item.PaymentFeeAmount);
                item.OrderTotalString = _currencyService.FormatCurrency(item.OrderTotal);

                item.OrderLineTaxAmountString = _currencyService.FormatCurrency(item.OrderLineTaxAmount);
                item.OrderLineProductPriceString = _currencyService.FormatCurrency(item.ProductPrice);
                item.OrderLineDiscountAmountString = _currencyService.FormatCurrency(item.OrderLineDiscountAmount);
                item.OrderLineTotalString = _currencyService.FormatCurrency(item.OrderLineTotal);
                item.OrderLineRowTotalString = _currencyService.FormatCurrency(item.OrderLineRowTotal);
            }

            var csvString = CsvConverter.ExportCsv(orderItems);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "order-lines-export.csv");
        }
    }
}
