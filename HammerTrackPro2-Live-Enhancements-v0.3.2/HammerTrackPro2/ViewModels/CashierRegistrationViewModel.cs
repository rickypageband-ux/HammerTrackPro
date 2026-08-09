using HammerTrackPro2.Models;

namespace HammerTrackPro2.ViewModels;

public sealed class CashierRegistrationViewModel
{
    public CashierRegistrationViewModel(bool isSeller, IReadOnlyList<RegisteredBidderSummary>? registeredBidders = null)
    {
        IsSeller = isSeller;
        RegisteredBidders = registeredBidders ?? Array.Empty<RegisteredBidderSummary>();
    }

    public bool IsSeller { get; }
    public IReadOnlyList<RegisteredBidderSummary> RegisteredBidders { get; }
    public AuctionBidder? Buyer { get; private set; }
    public Consignor? Seller { get; private set; }

    public void Create(int number,string businessName,string firstName,string lastName,string phone,string email,string address="",string city="",string state="",string zip="",string driversLicense="",string resaleLicenseNumber="",string resaleLicenseState="",bool taxExempt=false)
    {
        if (IsSeller && string.IsNullOrWhiteSpace(businessName) && string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            throw new InvalidOperationException("Enter a business name or person name.");
        if (!IsSeller && RegisteredBidders.Any(item => item.BuyerNumber == number))
            throw new InvalidOperationException($"Buyer Number #{number} is already registered in the current auction. Choose an available number from the list.");

        if (IsSeller)
            Seller = new Consignor { ConsignorNumber=number,BusinessName=businessName.Trim(),FirstName=firstName.Trim(),LastName=lastName.Trim(),Phone=phone.Trim(),Email=email.Trim(),Active=true };
        else
            Buyer = new AuctionBidder { BidderNumber=number,FirstName=firstName.Trim(),LastName=lastName.Trim(),Address=address.Trim(),City=city.Trim(),State=state.Trim(),Zip=zip.Trim(),Phone=phone.Trim(),Email=email.Trim(),DriversLicense=driversLicense.Trim(),ResaleLicenseNumber=resaleLicenseNumber.Trim(),ResaleLicenseState=resaleLicenseState.Trim(),TaxExempt=taxExempt };
    }
}
