using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.Tests;

internal sealed class InMemoryConsignorRepository : IConsignorRepository
{
    private readonly List<Consignor> _items = new();

    public IReadOnlyList<Consignor> GetConsignors() => _items.ToList();

    public void SaveConsignor(Consignor consignor, int? originalConsignorNumber)
    {
        if (originalConsignorNumber.HasValue)
            _items.RemoveAll(item => item.ConsignorNumber == originalConsignorNumber.Value);
        if (consignor.ConsignorId == 0) consignor.ConsignorId = _items.Count + 1;
        _items.Add(consignor);
    }

    public void DeleteConsignor(int consignorNumber) =>
        _items.RemoveAll(item => item.ConsignorNumber == consignorNumber);

    public void AssociateConsignorWithCurrentAuction(int consignorNumber) { }
}
