using CatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogApi.Data.Repositories;

public class ProductRepository(ProductsDbContext dbContext) : IProductRepository
{
    private readonly ProductsDbContext _dbContext = dbContext;

    public async Task<Product?> GetByIdAsync(int id)
        => await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(List<Product> Items, int TotalCount)> GetPageAsync(int page, int pageSize)
    {
        var totalCount = await _dbContext.Products.CountAsync();
        var items = await _dbContext.Products
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync();
        return product;
    }

    public async Task DeleteAsync(int id)
    {
        var product = await GetByIdAsync(id);
        if (product is not null)
        {
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
        => await _dbContext.Products.AnyAsync(p => p.Id == id);
}
