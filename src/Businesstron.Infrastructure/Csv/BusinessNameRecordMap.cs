using CsvHelper.Configuration;

namespace Businesstron.Infrastructure.Csv;

/// <summary>Reproduces the exact CSV column layout the client already works with.</summary>
public sealed class BusinessNameRecordMap : ClassMap<BusinessNameRecord>
{
    public BusinessNameRecordMap()
    {
        Map(m => m.BusinessName).Name("Business Name");
        Map(m => m.Status).Name("Status");
        Map(m => m.RegistrationDate).Name("Registration Date");
        Map(m => m.RenewalDate).Name("Renewal Date");
        Map(m => m.CancelledDate).Name("Cancelled Date");
        Map(m => m.CancellationUnderReview).Name("Cancellation Under Review");
        Map(m => m.AddressForServiceDocuments).Name("Address For Service Documents");
        Map(m => m.PrincipalPlaceOfBusiness).Name("Principal Place Of Business");
        Map(m => m.HolderName).Name("Holder Name");
        Map(m => m.HolderType).Name("Holder Type");
        Map(m => m.HolderAbn).Name("Holder ABN");
        Map(m => m.AbnStatus).Name("ABN Status");
        Map(m => m.Gst).Name("Goods & Services Tax (GST)");
        Map(m => m.HasMultipleBusinessNames).Name("Has Multiple Business Names");
    }
}
