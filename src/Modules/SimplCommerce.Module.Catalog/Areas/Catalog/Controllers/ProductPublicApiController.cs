using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("api/v1/products")]
    public class ProductPublicApiController : Controller
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IProductPricingService _productPricingService;
        private readonly IMediaService _mediaService;

        public ProductPublicApiController(
            IRepository<Product> productRepository,
            IProductPricingService productPricingService,
            IMediaService mediaService)
        {
            _productRepository = productRepository;
            _productPricingService = productPricingService;
            _mediaService = mediaService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            string search = null,
            long categoryId = 0,
            long brandId = 0,
            decimal minPrice = 0,
            decimal maxPrice = decimal.MaxValue,
            string sortBy = "Id",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 20)
        {
            var query = _productRepository.Query()
                .Where(p => !p.IsDeleted && p.IsPublished && p.IsAllowToOrder);

            // 搜索过滤
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
            }

            // 分类过滤
            if (categoryId > 0)
            {
                query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId));
            }

            // 品牌过滤
            if (brandId > 0)
            {
                query = query.Where(p => p.BrandId == brandId);
            }

            // 价格范围过滤
            query = query.Where(p => p.Price >= minPrice && p.Price <= maxPrice);

            // 排序
            query = sortOrder.ToLower() == "asc"
                ? query.OrderBy(p => EF.Property<object>(p, sortBy))
                : query.OrderByDescending(p => EF.Property<object>(p, sortBy));

            // 分页
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.ThumbnailImage)
                .Include(p => p.Brand)
                .ToListAsync();

            // 转换为视图模型
            var productVms = products.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Price = p.Price,
                OldPrice = p.OldPrice,
                ShortDescription = p.ShortDescription,
                LongDescription = p.Description,
                ThumbnailUrl = p.ThumbnailImage != null ? _mediaService.GetMediaUrl(p.ThumbnailImage) : null,
                Brand = p.Brand != null ? new { Id = p.Brand.Id, Name = p.Brand.Name } : null,
                IsAllowToOrder = p.IsAllowToOrder,
                StockQuantity = p.StockQuantity
            }).ToList();

            var result = new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = productVms
            };

            return Ok(result);
        }
    }
}