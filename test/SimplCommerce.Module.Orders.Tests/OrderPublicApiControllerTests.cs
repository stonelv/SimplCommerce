using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Orders.Areas.Orders.Controllers;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;
using Xunit;
using System.Linq;

namespace SimplCommerce.Module.Orders.Tests
{
    public class OrderPublicApiControllerTests
    {
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<ICartService> _cartServiceMock;
        private readonly Mock<IWorkContext> _workContextMock;
        private readonly Mock<IRepository<CartItem>> _cartItemRepositoryMock;
        private readonly Mock<IRepository<Product>> _productRepositoryMock;
        private readonly Mock<IRepository<Order>> _orderRepositoryMock;
        private readonly OrderPublicApiController _controller;

        public OrderPublicApiControllerTests()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _cartServiceMock = new Mock<ICartService>();
            _workContextMock = new Mock<IWorkContext>();
            _cartItemRepositoryMock = new Mock<IRepository<CartItem>>();
            _productRepositoryMock = new Mock<IRepository<Product>>();
            _orderRepositoryMock = new Mock<IRepository<Order>>();
            _controller = new OrderPublicApiController(
                _orderServiceMock.Object,
                _cartServiceMock.Object,
                _workContextMock.Object,
                _cartItemRepositoryMock.Object,
                _productRepositoryMock.Object,
                _orderRepositoryMock.Object);
        }

        [Fact]
        public async Task CreateOrder_ValidCart_ReturnsOk()
        {
            // Arrange
            var user = new User { Id = 1 };
            var product = new Product { Name = "Test Product", Price = 100, IsPublished = true, IsAllowToOrder = true };
            var cartItem = new CartItem { CustomerId = 1, ProductId = 1, Quantity = 2, Product = product };

            _workContextMock.Setup(w => w.GetCurrentUser())
                .ReturnsAsync(user);

            _cartItemRepositoryMock.Setup(r => r.Query())
                .Returns(new[] { cartItem }.AsQueryable());

            // Act
            var result = await _controller.CreateOrder(new CreateOrderRequest { PaymentMethod = "Alipay" });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_EmptyCart_ReturnsBadRequest()
        {
            // Arrange
            var user = new User { Id = 1 };

            _workContextMock.Setup(w => w.GetCurrentUser())
                .ReturnsAsync(user);

            _cartItemRepositoryMock.Setup(r => r.Query())
                .Returns(Array.Empty<CartItem>().AsQueryable());

            // Act
            var result = await _controller.CreateOrder(new CreateOrderRequest { PaymentMethod = "Alipay" });

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_InsufficientStock_ReturnsBadRequest()
        {
            // Arrange
            var user = new User { Id = 1 };
            var product = new Product { Name = "Test Product", Price = 100, IsPublished = true, IsAllowToOrder = true, StockTrackingIsEnabled = true, StockQuantity = 1 };
            var cartItem = new CartItem { CustomerId = 1, ProductId = 1, Quantity = 2, Product = product };

            _workContextMock.Setup(w => w.GetCurrentUser())
                .ReturnsAsync(user);

            _cartItemRepositoryMock.Setup(r => r.Query())
                .Returns(new[] { cartItem }.AsQueryable());

            // Act
            var result = await _controller.CreateOrder(new CreateOrderRequest { PaymentMethod = "Alipay" });

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }
    }
}