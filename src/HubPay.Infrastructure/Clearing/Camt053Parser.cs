using System.Xml.Linq;
using HubPay.Domain.Interfaces;

namespace HubPay.Infrastructure.Clearing;

public sealed class Camt053Parser : ICamt053Parser
{
    private static readonly XNamespace Ns = "urn:iso:std:iso:20022:tech:xsd:camt.053.001.08";

    public IReadOnlyList<Camt053Entry> ParseEntries(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var entries = new List<Camt053Entry>();

        foreach (var entry in doc.Descendants(Ns + "Ntry"))
        {
            var endToEndId = entry.Descendants(Ns + "EndToEndId").FirstOrDefault()?.Value;
            var amountEl = entry.Descendants(Ns + "Amt").FirstOrDefault();
            if (endToEndId is null || amountEl is null) continue;
            if (!decimal.TryParse(amountEl.Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount)) continue;

            var currency = amountEl.Attribute("Ccy")?.Value ?? "EUR";
            entries.Add(new Camt053Entry(endToEndId, amount, currency));
        }

        return entries;
    }
}
