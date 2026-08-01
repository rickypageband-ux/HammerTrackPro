using HammerTrackPro2.Models;
namespace HammerTrackPro2.Services;
public sealed class ConsignorService
{
    private readonly IConsignorRepository _repository;
    public ConsignorService(IConsignorRepository repository) => _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    public IReadOnlyList<Consignor> GetConsignors() => _repository.GetConsignors();
    public void SaveConsignor(Consignor consignor, int? originalNumber)
    {
        ArgumentNullException.ThrowIfNull(consignor);
        if (consignor.ConsignorNumber <= 0) throw new InvalidOperationException("Consignor number must be greater than zero.");
        if (string.IsNullOrWhiteSpace(consignor.BusinessName) && string.IsNullOrWhiteSpace(consignor.FirstName) && string.IsNullOrWhiteSpace(consignor.LastName)) throw new InvalidOperationException("Enter a business name or contact name.");
        if (consignor.DefaultCommissionPercent is < 0 or > 100 || consignor.CommissionOverridePercent is < 0 or > 100) throw new InvalidOperationException("Commission percentages must be between 0 and 100.");
        _repository.SaveConsignor(consignor, originalNumber);
        _repository.AssociateConsignorWithCurrentAuction(consignor.ConsignorNumber);
    }
    public void DeleteConsignor(int number)
    {
        if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
        _repository.DeleteConsignor(number);
    }
}
