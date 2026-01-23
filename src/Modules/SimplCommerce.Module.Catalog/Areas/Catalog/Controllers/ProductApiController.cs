using System; using System.Linq; using System.Threading.Tasks; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using SimplCommerce.Infrastructure.Data; using SimplCommerce.Infrastructure.Web.SmartTable; using SimplCommerce.Module.Catalog.Models; using SimplCommerce.Module.Catalog.Services; using SimplCommerce.Module.Core.Services; using SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels; using Microsoft.Extensions.Configuration;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("api/v1/products")]
    public class ProductApiController : Controller
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IMediaService _mediaService;
        private readonly IProductPricingService _productPricingService;
        private readonly bool _isProductPriceIncludeTax;
        private readonly ICurrencyService _currencyService;

        public ProductApiController(
            IRepository<Product> productRepository,
            IMediaService mediaService,
            IProductPricingService productPricingService,
            IConfiguration config,
            ICurrencyService currencyService)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _productPricingService = productPricingService;
            _isProductPriceIncludeTax = config.GetValue<bool>("Catalog.IsProductPriceIncludeTax");
            _currencyService = currencyService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string sort = "createdOn", [FromQuery] string order = "desc", [FromQuery] long? categoryId = null, [FromQuery] long? brandId = null, [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null, [FromQuery] string q = null)
        {
            var query = _productRepository.Query().Where(p => p.IsPublished && !p.IsDeleted);

            // 过滤
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId.Value));
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Name.Contains(q) || p.ShortDescription.Contains(q) || p.Description.Contains(q));
            }

            // 排序
            switch (sort.ToLower())
            {
                case "price":
                    query = order.ToLower() == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price);
                    break;
                case "name":
                    query = order.ToLower() == "asc" ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name);
                    break;
                case "createdon":
                default:
                    query = order.ToLower() == "asc" ? query.OrderBy(p => p.CreatedOn) : query.OrderByDescending(p => p.CreatedOn);
                    break;
            }

            // 分页
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.ThumbnailImage)
                .Include(p => p.Brand)
                .ToListAsync();

            // 转换为ViewModel
            var productVms = products.Select(p =>
            {
                var productThumbnail = ProductThumbnail.FromProduct(p);
                productThumbnail.ThumbnailUrl = _mediaService.GetThumbnailUrl(p.ThumbnailImage);
                productThumbnail.CalculatedProductPrice = _productPricingService.CalculateProductPrice(p);
                return productThumbnail;
            }).ToList();

            var result = new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Items = productVms
            };

            return Ok(result);
        }
    }
}