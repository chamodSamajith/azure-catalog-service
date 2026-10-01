using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CatalogApi.Data;
using CatalogApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogApi.Tests;

public class ProductEndpointsTests : IAsyncLifetime
{
    private readonly CatalogApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    #region Health Checks

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Create Product

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedWithLocation()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 99.99m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var createdProduct = await response.Content.ReadAsAsync<ProductResponse>();
        Assert.NotNull(createdProduct);
        Assert.Equal("Test Product", createdProduct.Name);
        Assert.Equal(99.99m, createdProduct.Price);
    }

    [Fact]
    public async Task CreateProduct_WithMissingName_ReturnsBadRequest()
    {
        var request = new CreateProductRequest
        {
            Name = "",
            Description = "A test product",
            Price = 99.99m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadAsAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation Failed", problem.Title);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPrice_ReturnsBadRequest()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "A test product",
            Price = -10m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithLongName_ReturnsBadRequest()
    {
        var request = new CreateProductRequest
        {
            Name = new string('a', 101),
            Description = "A test product",
            Price = 99.99m
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Get Product

    [Fact]
    public async Task GetProduct_WithValidId_ReturnsProduct()
    {
        // Arrange: Create a product first
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 99.99m
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createdProduct = await createResponse.Content.ReadAsAsync<ProductResponse>();
        var productId = createdProduct!.Id;

        // Act: Get the product
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadAsAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.Equal(productId, product.Id);
        Assert.Equal("Test Product", product.Name);
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadAsAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Not Found", problem.Title);
    }

    #endregion

    #region Get Products (Paginated)

    [Fact]
    public async Task GetProducts_ReturnsPagedList()
    {
        var response = await _client.GetAsync("/api/products?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<ProductsPageResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetProducts_WithInvalidPage_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/products?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithInvalidPageSize_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/products?page=1&pageSize=-5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Update Product

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
    {
        // Arrange: Create a product first
        var createRequest = new CreateProductRequest
        {
            Name = "Original Name",
            Description = "Original description",
            Price = 50m
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createdProduct = await createResponse.Content.ReadAsAsync<ProductResponse>();
        var productId = createdProduct!.Id;

        // Act: Update the product
        var updateRequest = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Price = 99.99m
        };
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedProduct = await response.Content.ReadAsAsync<ProductResponse>();
        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Name", updatedProduct.Name);
        Assert.Equal(99.99m, updatedProduct.Price);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidId_ReturnsNotFound()
    {
        var updateRequest = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Price = 99.99m
        };

        var response = await _client.PutAsJsonAsync("/api/products/999", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange: Create a product first
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 50m
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createdProduct = await createResponse.Content.ReadAsAsync<ProductResponse>();
        var productId = createdProduct!.Id;

        // Act: Update with invalid data
        var updateRequest = new UpdateProductRequest
        {
            Name = "",
            Description = "Updated description",
            Price = 99.99m
        };
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Delete Product

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContent()
    {
        // Arrange: Create a product first
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 99.99m
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createdProduct = await createResponse.Content.ReadAsAsync<ProductResponse>();
        var productId = createdProduct!.Id;

        // Act: Delete the product
        var response = await _client.DeleteAsync($"/api/products/{productId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the product is deleted
        var getResponse = await _client.GetAsync($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}

public static class HttpContentExtensions
{
    public static async Task<T?> ReadAsAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
