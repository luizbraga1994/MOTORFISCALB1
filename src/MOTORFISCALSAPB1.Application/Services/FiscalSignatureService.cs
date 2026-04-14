using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Services;

public sealed class FiscalSignatureService : IFiscalSignatureService
{
    public FiscalSignature Build(FiscalResolutionResult r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new FiscalSignature(
            r.Cfop,
            r.CstIcms,
            r.AliquotaIcms,
            r.CstIpi,
            r.AliquotaIpi,
            r.CstPis,
            r.AliquotaPis,
            r.CstCofins,
            r.AliquotaCofins,
            r.TemSt,
            r.AliquotaStInterna,
            r.MvaSt);
    }
}
