using Inventory_Management_System.Dtos;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IRecieveProduct
    {
        Task<bool> RecieveProduct(RecieveProductDto Rdto);

    }
}
