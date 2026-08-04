using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
namespace HammerTrackPro2.ViewModels;
public sealed class ConsignorViewModel
{
    private readonly ConsignorService _service;
    private List<Consignor> _all = new();
    private int? _originalNumber;
    public ConsignorViewModel(ConsignorService service) => _service = service ?? throw new ArgumentNullException(nameof(service));
    public IReadOnlyList<Consignor> Consignors { get; private set; } = Array.Empty<Consignor>();
    public string SearchText { get; set; } = string.Empty;
    public Consignor Editor { get; private set; } = new();
    public void Load() { _all = _service.GetConsignors().OrderBy(x => x.ConsignorNumber).ToList(); Search(); }
    public void Search()
    {
        var q = SearchText.Trim();
        Consignors = _all.Where(x => q.Length == 0 || x.ConsignorNumber.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) || x.BusinessName.Contains(q, StringComparison.OrdinalIgnoreCase) || x.ContactName.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Phone.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(q, StringComparison.OrdinalIgnoreCase) || x.TaxId.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    public void BeginNew() { _originalNumber = null; Editor = new Consignor { ConsignorNumber = _all.Count == 0 ? 1 : _all.Max(x => x.ConsignorNumber) + 1, Active = true, PrintCheck = true }; }
    public void BeginEdit(Consignor consignor) { ArgumentNullException.ThrowIfNull(consignor); _originalNumber = consignor.ConsignorNumber; Editor = new Consignor { ConsignorId=consignor.ConsignorId, LinkedBuyerId=consignor.LinkedBuyerId, ConsignorNumber=consignor.ConsignorNumber, BusinessName=consignor.BusinessName, FirstName=consignor.FirstName, LastName=consignor.LastName, Address=consignor.Address, City=consignor.City, State=consignor.State, Zip=consignor.Zip, Phone=consignor.Phone, Email=consignor.Email, TaxId=consignor.TaxId, DefaultCommissionPercent=consignor.DefaultCommissionPercent, CommissionOverridePercent=consignor.CommissionOverridePercent, Notes=consignor.Notes, Active=consignor.Active, PrintCheck=consignor.PrintCheck, HoldCheck=consignor.HoldCheck, TaxExempt=consignor.TaxExempt, Require1099=consignor.Require1099, CreatedDate=consignor.CreatedDate }; }
    public void Save() { if (_all.Any(x => x.ConsignorNumber == Editor.ConsignorNumber && x.ConsignorNumber != _originalNumber)) throw new InvalidOperationException("That seller number is already in use."); _service.SaveConsignor(Editor, _originalNumber); Load(); BeginNew(); }
    public void Delete() { if (!_originalNumber.HasValue) throw new InvalidOperationException("Select a seller to delete."); _service.DeleteConsignor(_originalNumber.Value); Load(); BeginNew(); }
}
