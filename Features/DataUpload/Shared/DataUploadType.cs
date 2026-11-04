namespace Inventory_Management_System.Features.DataUpload.Shared
{
    // The kinds of data a user can upload from an Excel file.
    // The numbers are sent by the frontend, so never change or reuse them.
    public enum DataUploadType
    {
        Brand = 1,
        Category = 2,
        SubCategory = 3,
        Product = 4,
        Supplier = 5,
        Customer = 6,
        ExpenseCategory = 7,
    }

    /// <summary>One column of an upload sheet, as the system expects it.</summary>
    /// <param name="Order">Position in the template (1 = column A). Also the id used in a column mapping.</param>
    /// <param name="Name">Heading text in the template.</param>
    /// <param name="Required">A row without a value here is rejected.</param>
    /// <param name="Hint">Short help shown in the template and on the page.</param>
    /// <param name="MaxLength">Longest text the database column accepts (null = no limit / not text).</param>
    public sealed record UploadColumn(int Order, string Name, bool Required, string Hint, int? MaxLength = null);

    /// <summary>Everything the system knows about one upload type.</summary>
    public sealed record UploadTypeDefinition(
        DataUploadType Type,
        string Name,
        string Description,
        // Permission needed to upload this type. Null = any logged-in user (same as the screen it fills).
        string? Permission,
        IReadOnlyList<UploadColumn> Columns)
    {
        public UploadColumn? Column(int order) => Columns.FirstOrDefault(c => c.Order == order);
    }

    /// <summary>A row that failed, with every reason it failed.</summary>
    public sealed record RowError(int RowNumber, IReadOnlyList<string> Messages);
}
