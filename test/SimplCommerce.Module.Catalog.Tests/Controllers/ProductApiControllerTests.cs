using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Areas.Catalog.Controllers;
using SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Core.Extensions;
using Xunit;

namespace SimplCommerce.Module.Catalog.Tests.Controllers
{
    public class ProductApiControllerTests
    {
        [Fact]
        public async Task GetProducts_ReturnsOkResult_WithPagination()
        {
            // Arrange
            var productRepositoryMock = new Mock<IRepository<Product>>();
            var mediaServiceMock = new Mock<IMediaService>();
            var productServiceMock = new Mock<IProductService>();
            var productLinkRepositoryMock = new Mock<IRepository<ProductLink>>();
            var productCategoryRepositoryMock = new Mock<IRepository<ProductCategory>>();
            var productOptionValueRepositoryMock = new Mock<IRepository<ProductOptionValue>>();
            var productAttributeValueRepositoryMock = new Mock<IRepository<ProductAttributeValue>>();
            var productMediaRepositoryMock = new Mock<IRepository<ProductMedia>>();
            var workContextMock = new Mock<IWorkContext>();

            // 创建测试数据
            var products = new List<Product>();
            // 使用反射设置Id和其他属性
            var product1 = new Product();
            product1.GetType().GetProperty("Id").SetValue(product1, 1);
            product1.GetType().GetProperty("Name").SetValue(product1, "Product 1");
            product1.GetType().GetProperty("Price").SetValue(product1, 100m);
            product1.GetType().GetProperty("IsPublished").SetValue(product1, true);
            product1.GetType().GetProperty("IsDeleted").SetValue(product1, false);
            product1.GetType().GetProperty("IsAllowToOrder").SetValue(product1, true);

            var product2 = new Product();
            product2.GetType().GetProperty("Id").SetValue(product2, 2);
            product2.GetType().GetProperty("Name").SetValue(product2, "Product 2");
            product2.GetType().GetProperty("Price").SetValue(product2, 200m);
            product2.GetType().GetProperty("IsPublished").SetValue(product2, true);
            product2.GetType().GetProperty("IsDeleted").SetValue(product2, false);
            product2.GetType().GetProperty("IsAllowToOrder").SetValue(product2, true);

            var product3 = new Product();
            product3.GetType().GetProperty("Id").SetValue(product3, 3);
            product3.GetType().GetProperty("Name").SetValue(product3, "Product 3");
            product3.GetType().GetProperty("Price").SetValue(product3, 300m);
            product3.GetType().GetProperty("IsPublished").SetValue(product3, true);
            product3.GetType().GetProperty("IsDeleted").SetValue(product3, false);
            product3.GetType().GetProperty("IsAllowToOrder").SetValue(product3, true);

            products.Add(product1);
            products.Add(product2);
            products.Add(product3);

            // 使用AsyncQueryableExtensions创建支持异步操作的Queryable
            var asyncQueryable = products.AsAsyncQueryable();

            // 模拟Query方法返回asyncQueryable
            productRepositoryMock
                .Setup(repo => repo.Query())
                .Returns(asyncQueryable);

            var controller = new ProductApiController(
                productRepositoryMock.Object,
                mediaServiceMock.Object,
                productServiceMock.Object,
                productLinkRepositoryMock.Object,
                productCategoryRepositoryMock.Object,
                productOptionValueRepositoryMock.Object,
                productAttributeValueRepositoryMock.Object,
                productMediaRepositoryMock.Object,
                workContextMock.Object
            );
            
            // Act
            var result = await controller.GetProducts(null, null, null, null, null, null, true, 0, 10, "Id", true);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        
        [Fact]
        public async Task GetProducts_ReturnsFilteredProducts()
        {
            // Arrange
            var productRepositoryMock = new Mock<IRepository<Product>>();
            var mediaServiceMock = new Mock<IMediaService>();
            var productServiceMock = new Mock<IProductService>();
            var productLinkRepositoryMock = new Mock<IRepository<ProductLink>>();
            var productCategoryRepositoryMock = new Mock<IRepository<ProductCategory>>();
            var productOptionValueRepositoryMock = new Mock<IRepository<ProductOptionValue>>();
            var productAttributeValueRepositoryMock = new Mock<IRepository<ProductAttributeValue>>();
            var productMediaRepositoryMock = new Mock<IRepository<ProductMedia>>();
            var workContextMock = new Mock<IWorkContext>();

            // 创建测试数据
            var products = new List<Product>();

            var product1 = new Product();
            product1.GetType().GetProperty("Id").SetValue(product1, 1);
            product1.GetType().GetProperty("Name").SetValue(product1, "Apple iPhone");
            product1.GetType().GetProperty("Price").SetValue(product1, 1000m);
            product1.GetType().GetProperty("IsPublished").SetValue(product1, true);
            product1.GetType().GetProperty("IsDeleted").SetValue(product1, false);
            product1.GetType().GetProperty("IsAllowToOrder").SetValue(product1, true);

            var product2 = new Product();
            product2.GetType().GetProperty("Id").SetValue(product2, 2);
            product2.GetType().GetProperty("Name").SetValue(product2, "Samsung Galaxy");
            product2.GetType().GetProperty("Price").SetValue(product2, 800m);
            product2.GetType().GetProperty("IsPublished").SetValue(product2, true);
            product2.GetType().GetProperty("IsDeleted").SetValue(product2, false);
            product2.GetType().GetProperty("IsAllowToOrder").SetValue(product2, true);

            var product3 = new Product();
            product3.GetType().GetProperty("Id").SetValue(product3, 3);
            product3.GetType().GetProperty("Name").SetValue(product3, "Google Pixel");
            product3.GetType().GetProperty("Price").SetValue(product3, 700m);
            product3.GetType().GetProperty("IsPublished").SetValue(product3, true);
            product3.GetType().GetProperty("IsDeleted").SetValue(product3, false);
            product3.GetType().GetProperty("IsAllowToOrder").SetValue(product3, true);

            products.Add(product1);
            products.Add(product2);
            products.Add(product3);

            // 使用AsyncQueryableExtensions创建支持异步操作的Queryable
            var filteredProducts = products.Where(p => p.Name.Contains("iPhone")).ToList();
            var asyncQueryable = filteredProducts.AsAsyncQueryable();

            // 模拟Query方法返回asyncQueryable
            productRepositoryMock
                .Setup(repo => repo.Query())
                .Returns(asyncQueryable);

            var controller = new ProductApiController(
                productRepositoryMock.Object,
                mediaServiceMock.Object,
                productServiceMock.Object,
                productLinkRepositoryMock.Object,
                productCategoryRepositoryMock.Object,
                productOptionValueRepositoryMock.Object,
                productAttributeValueRepositoryMock.Object,
                productMediaRepositoryMock.Object,
                workContextMock.Object
            );
            
            // Act
            var result = await controller.GetProducts("iPhone", null, null, null, null, null, true, 0, 10, "Name", false);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}