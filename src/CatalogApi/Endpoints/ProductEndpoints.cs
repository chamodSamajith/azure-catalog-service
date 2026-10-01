using CatalogApi.Data.Repositories;
using CatalogApi.DTOs;
using CatalogApi.Models;
using FluentValidation;

namespace CatalogApi.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithName("Products")
            .WithOpenApi();

        group.MapGet("/", GetProducts)
            .WithName("GetProducts")
            .WithDescription("Get a paginated list of products");

        group.MapGet("/{id}", GetProductById)
            .WithName("GetProductById")
            .WithDescription("Get a product by ID");

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithDescription("Create a new product")
            .Accepts<CreateProductRequest>("application/json");

        group.MapPut("/{id}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithDescription("Update an existing product")
            .Accepts<UpdateProductRequest>("application/json");

        group.MapDelete("/{id}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithDescription("Delete a product");
    }

    private static async Task<IResult> GetProducts(
        IProductRepository repository,
        int page = 1,
        int pageSize = 20)
    {
        if (page <= 0 || pageSize <= 0)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Parameters",
                Detail = "Page and pageSize must be greater than 0",
                Status = StatusCodes.Status400BadRequest
            });

        var (items, totalCount) = await repository.GetPageAsync(page, pageSize);
        var response = new ProductsPageResponse
        {
            Items = items.ConvertAll(p => MapToResponse(p)),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetProductById(
        int id,
        IProductRepository repository)
    {
        var product = await repository.GetByIdAsync(id);
        if (product is null)
            return Results.NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Product with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });

        return Results.Ok(MapToResponse(product));
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        IValidator<CreateProductRequest> validator,
        IProductRepository repository)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                Status = StatusCodes.Status400BadRequest
            });

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(product);
        var response = MapToResponse(created);

        return Results.Created($"/api/products/{created.Id}", response);
    }

    private static async Task<IResult> UpdateProduct(
        int id,
        UpdateProductRequest request,
        IValidator<UpdateProductRequest> validator,
        IProductRepository repository)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                Status = StatusCodes.Status400BadRequest
            });

        var product = await repository.GetByIdAsync(id);
        if (product is null)
            return Results.NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Product with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;

        var updated = await repository.UpdateAsync(product);
        return Results.Ok(MapToResponse(updated));
    }

    private static async Task<IResult> DeleteProduct(
        int id,
        IProductRepository repository)
    {
        var exists = await repository.ExistsAsync(id);
        if (!exists)
            return Results.NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Product with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });

        await repository.DeleteAsync(id);
        return Results.NoContent();
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        CreatedAtUtc = product.CreatedAtUtc
    };
}
