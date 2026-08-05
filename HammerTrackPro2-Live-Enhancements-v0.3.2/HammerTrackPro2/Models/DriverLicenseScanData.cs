namespace HammerTrackPro2.Models;

public sealed record DriverLicenseScanData(
    string FirstName,
    string LastName,
    string Address,
    string City,
    string State,
    string Zip,
    string DriversLicense);
