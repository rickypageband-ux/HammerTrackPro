using HammerTrackPro2.Models;
namespace HammerTrackPro2.Services;
public interface IConsignorRepository
{
    IReadOnlyList<Consignor> GetConsignors();
    void SaveConsignor(Consignor consignor, int? originalConsignorNumber);
    void DeleteConsignor(int consignorNumber);
    void AssociateConsignorWithCurrentAuction(int consignorNumber);
}
