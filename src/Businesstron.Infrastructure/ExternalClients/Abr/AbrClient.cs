using AngleSharp.Html.Parser;
using Businesstron.Application.Common.Interfaces;

namespace Businesstron.Infrastructure.ExternalClients.Abr;

/// <summary>Scrapes ABN status and GST registration from abr.business.gov.au.</summary>
public sealed class AbrClient(HttpClient http) : IAbrClient
{
    private readonly HtmlParser _htmlParser = new();

    public async Task<AbrDetails> GetAbnDetailsAsync(string abn, CancellationToken cancellationToken)
    {
        var content = await http.GetStringAsync($"ABN/View?id={abn}", cancellationToken);

        var document = await _htmlParser.ParseDocumentAsync(content, cancellationToken);

        var abnStatus = document.QuerySelectorAll("th")
            .FirstOrDefault(x => x.TextContent.Contains("ABN status:"))?
            .NextElementSibling?.TextContent.Trim(' ', '"', '\n');

        var gst = document.QuerySelectorAll("th")
            .FirstOrDefault(x => x.TextContent.Contains("Goods & Services Tax (GST):"))?
            .NextElementSibling?.TextContent.Trim(' ', '"', '\n');

        return new AbrDetails(abnStatus, gst);
    }
}
