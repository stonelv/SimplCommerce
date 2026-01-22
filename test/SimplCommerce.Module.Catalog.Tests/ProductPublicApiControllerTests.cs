using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Areas.Catalog.Controllers;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;
using Xunit;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace SimplCommerce.Module.Catalog.Tests
{
    public class ProductPublicApiControllerTests
    {
        private readonly Mock<IRepository<Product>> _productRepositoryMock;
        private readonly Mock<IProductPricingService> _productPricingServiceMock;
        private readonly Mock<IMediaService> _mediaServiceMock;
        private readonly ProductPublicApiController _controller;

        public ProductPublicApiControllerTests()
        {
            _productRepositoryMock = new Mock<IRepository<Product>>();
            _productPricingServiceMock = new Mock<IProductPricingService>();
            _mediaServiceMock = new Mock<IMediaService>();
            _controller = new ProductPublicApiController(
                _productRepositoryMock.Object,
                _productPricingServiceMock.Object,
                _mediaServiceMock.Object);
        }

        [Fact]
        public async Task Get_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Name = "Product 1", Price = 100, IsPublished = true, IsAllowToOrder = true },
                new Product { Name = "Product 2", Price = 200, IsPublished = true, IsAllowToOrder = true }
            };

            _productRepositoryMock.Setup(r => r.Query())
                .Returns(products.AsQueryable());

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var response = okResult.Value.GetType().GetProperty("Items").GetValue(okResult.Value);
            var items = (System.Collections.IEnumerable)response;
            Assert.Equal(2, items.Cast<object>().Count());
        }

        [Fact]
        public async Task Get_WithSearchKeyword_ReturnsFilteredProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Name = "Product 1", Price = 100, IsPublished = true, IsAllowToOrder = true },
                new Product { Name = "Test Product", Price = 200, IsPublished = true, IsAllowToOrder = true }
            };

            _productRepositoryMock.Setup(r => r.Query())
                .Returns(products.AsQueryable());

            // Act
            var result = await _controller.Get(search: "Test");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value.GetType().GetProperty("Items").GetValue(okResult.Value);
            var items = (System.Collections.IEnumerable)response;
            Assert.Single(items.Cast<object>());
        }

        [Fact]
        public async Task Get_WithCategoryFilter_ReturnsFilteredProducts()
        {
            // Arrange
            var category = new Category { Name = "Test Category" };
            var product = new Product { Name = "Product 1", Price = 100, IsPublished = true, IsAllowToOrder = true };
            product.Categories.Add(new ProductCategory { Product = product, Category = category });

            var products = new List<Product> { product };

            _productRepositoryMock.Setup(r => r.Query())
                .Returns(products.AsQueryable());

            // Act
            var result = await _controller.Get(categoryId: 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value.GetType().GetProperty("Items").GetValue(okResult.Value);
            var items = (System.Collections.IEnumerable)response;
            Assert.Single(items.Cast<object>());
        }
    }
}