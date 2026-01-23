using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("api/v1/products")]
    public class ProductPublicApiController : Controller
    {
        private readonly IProductService _productService;
        private readonly IRepository<Product> _productRepository;
        private readonly IMediaService _mediaService;

        public ProductPublicApiController(
            IProductService productService,
            IRepository<Product> productRepository,
            IMediaService mediaService)
        {
            _productService = productService;
            _productRepository = productRepository;
            _mediaService = mediaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string searchTerm = null,
            [FromQuery] long categoryId = 0,
            [FromQuery] long brandId = 0,
            [FromQuery] decimal minPrice = 0,
            [FromQuery] decimal maxPrice = decimal.MaxValue,
            [FromQuery] string sortBy = "name",
            [FromQuery] string sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _productRepository.Query()
                .Where(p => !p.IsDeleted && p.IsPublished && p.IsAllowToOrder);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.ShortDescription.Contains(searchTerm));
            }

            if (categoryId > 0)
            {
                query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId));
            }

            if (brandId > 0)
            {
                query = query.Where(p => p.BrandId == brandId);
            }

            query = query.Where(p => p.Price >= minPrice && p.Price <= maxPrice);

            query = sortBy.ToLower() switch
            {
                "price" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "name" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };

            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ShortDescription,
                    p.Price,
                    p.OldPrice,
                    p.SpecialPrice,
                    ThumbnailUrl = _mediaService.GetThumbnailUrl(p.ThumbnailImage),
                    p.IsFeatured
                })
                .ToListAsync();

            var result = new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = products
            };

            return Ok(result);
        }
    }
}