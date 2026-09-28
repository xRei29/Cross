using Core.Dto;

namespace Core.Domain;

public static class CatalogProductBuilder
{
    public static ImportResult<CatalogProduct> Build(ImportResult<ProductDto> importResult)
    {
        var items = new List<CatalogProduct>();
        var errors = new List<string>(importResult.Errors);

        foreach (ProductDto dto in importResult.Items)
        {
            try
            {
                items.Add(CatalogProduct.FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"{dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<CatalogProduct>(items, errors);
    }
}