using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Abstractions;

public interface IFiscalSignatureService
{
    FiscalSignature Build(FiscalResolutionResult result);
}
