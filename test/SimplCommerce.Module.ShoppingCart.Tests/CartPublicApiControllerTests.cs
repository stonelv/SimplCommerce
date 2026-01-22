using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;
using Xunit;
using System.Linq;

namespace SimplCommerce.Module.ShoppingCart.Tests
{
    public class CartPublicApiControllerTests
    {
        private readonly Mock<IRepository<CartItem>> _cartItemRepositoryMock;
        private readonly Mock<ICartService> _cartServiceMock;
        private readonly Mock<IWorkContext> _workContextMock;
        private readonly Mock<IRepository<Product>> _productRepositoryMock;
        private readonly CartPublicApiController _controller;

        public CartPublicApiControllerTests()
        {
            _cartItemRepositoryMock = new Mock<IRepository<CartItem>>();
            _cartServiceMock = new Mock<ICartService>();
            _workContextMock = new Mock<IWorkContext>();
            _productRepositoryMock = new Mock<IRepository<Product>>();
            _controller = new CartPublicApiController(
                _cartItemRepositoryMock.Object,
                _cartServiceMock.Object,
                _workContextMock.Object,
                _productRepositoryMock.Object);
        }

        [Fact]
        public async Task AddToCart_ValidProduct_ReturnsOk()
        {
            // Arrange
            var user = new User { Id = 1 };
            var product = new Product { Name = "Test Product", Price = 100, IsPublished = true, IsAllowToOrder = true };

            _workContextMock.Setup(w => w.GetCurrentUser())
                .ReturnsAsync(user);

            _productRepositoryMock.Setup(r => r.Query())
                .Returns(new[] { product }.AsQueryable());

            // Act
            var result = await _controller.AddToCart(1, new AddToCartRequest { ProductId = 1, Quantity = 1 });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task AddToCart_ProductNotFound_ReturnsBadRequest()
        {
            // Arrange
            var user = new User { Id = 1 };

            _workContextMock.Setup(w => w.GetCurrentUser())
                .ReturnsAsync(user);

            _productRepositoryMock.Setup(r => r.Query())
                .Returns(Array.Empty<Product>().AsQueryable());

            // Act
            var result = await _controller.AddToCart(1, new AddToCartRequest { ProductId = 999, Quantity = 1 });

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task AddToCart_InsufficientStock_ReturnsBadRequest()
        {
            // Arrange
            var user = new User { Id = 1 };
            var product = new Product { Name = "Test Product", Price = 100, IsPublished = true, IsAllowToOrder = true, StockTrackingIsEnabled = true, StockQuantity = 1 };

            _workContextMock.Setup(w => w.GetCurrentUser())
                .ReturnsAsync(user);

            _productRepositoryMock.Setup(r => r.Query())
                .Returns(new[] { product }.AsQueryable());

            // Act
            var result = await _controller.AddToCart(1, new AddToCartRequest { ProductId = 1, Quantity = 2 });

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }
    }
}