namespace Inventory_Management_System.Features.Clients.Shared
{
    // A client (customer organisation) the maintenance team looks after with this system.
    public sealed record ClientResponse(string Code, string Name, bool IsDefault);

    // The clients a Super Admin can choose from in the header.
    //
    // Today there is one installation and one client, so the list is fixed here and choosing a
    // client changes nothing else. To add a client, add a line. When clients get their own data
    // (separate databases or a tenant column), read this list from there instead: the endpoint
    // and the frontend dropdown stay the same.
    public static class ClientCatalog
    {
        public static readonly IReadOnlyList<ClientResponse> All =
        [
            new("smartit", "SmartIT", IsDefault: true),
        ];
    }
}
