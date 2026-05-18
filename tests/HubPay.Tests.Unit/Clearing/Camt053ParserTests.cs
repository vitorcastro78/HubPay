using HubPay.Infrastructure.Clearing;

namespace HubPay.Tests.Unit.Clearing;

public sealed class Camt053ParserTests
{
    [Fact]
    public void ParseEntries_ExtractsEndToEndAndAmount()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
              <BkToCstmrStmt>
                <Stmt>
                  <Ntry>
                    <Amt Ccy="EUR">42.50</Amt>
                    <NtryDtls><TxDtls><Refs><EndToEndId>E2E-TEST-001</EndToEndId></Refs></TxDtls></NtryDtls>
                  </Ntry>
                </Stmt>
              </BkToCstmrStmt>
            </Document>
            """;

        var parser = new Camt053Parser();
        var entries = parser.ParseEntries(xml);

        Assert.Single(entries);
        Assert.Equal("E2E-TEST-001", entries[0].EndToEndId);
        Assert.Equal(42.50m, entries[0].Amount);
    }
}
