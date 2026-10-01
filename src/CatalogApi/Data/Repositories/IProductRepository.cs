using CatalogApi.Models;

namespace CatalogApi.Data.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<(List<Product> Items, int TotalCount)> GetPageAsync(int page, int pageSize);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
