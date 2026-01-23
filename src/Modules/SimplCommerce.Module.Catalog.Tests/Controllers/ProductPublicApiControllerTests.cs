using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SimplCommerce.Module.Catalog.Areas.Catalog.Controllers;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;
using Xunit;

namespace SimplCommerce.Module.Catalog.Tests.Controllers
{
    public class ProductPublicApiControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<IRepository<Product>> _productRepositoryMock;
        private readonly Mock<IMediaService> _mediaServiceMock;
        private readonly ProductPublicApiController _controller;

        public ProductPublicApiControllerTests()
        {
            _productServiceMock = new Mock<IProductService>();
            _productRepositoryMock = new Mock<IRepository<Product>>();
            _mediaServiceMock = new Mock<IMediaService>();
            _controller = new ProductPublicApiController(_productServiceMock.Object, _productRepositoryMock.Object, _mediaServiceMock.Object);
        }

        [Fact]
        public async Task GetProducts_ReturnsOkResult()
        {
            var result = await _controller.GetProducts();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetProducts_WithSearchTerm_ReturnsFilteredResults()
        {
            var result = await _controller.GetProducts(searchTerm: "test");
            Assert.IsType<OkObjectResult>(result);
        }
    }
}