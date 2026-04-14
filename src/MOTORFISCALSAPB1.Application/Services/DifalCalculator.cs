using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Services;

/// <summary>
/// Calcula o DIFAL quando aplicável (operação interestadual para consumidor final,
/// contribuinte ou não, conforme EC 87/2015 e LC 190/2022).
/// </summary>
public interface IDifalCalculator
{
    void Apply(FiscalResolutionContext ctx, FiscalResolutionResult result);
}

public sealed class DifalCalculator : IDifalCalculator
{
    public void Apply(FiscalResolutionContext ctx, FiscalResolutionResult r)
    {
        if (!ctx.IsOperacaoInterestadual() || !ctx.ConsumidorFinal)
        {
            r.TemDifal = false;
            return;
        }

        // Se a regra não preencheu a interestadual, derivar do ICMS aplicado.
        var aliqInterestadual = r.AliquotaInterestadual ?? r.AliquotaIcms;

        // Alíquota interna de destino: quando não fornecida pela regra,
        // utiliza a alíquota ICMS da própria regra como base conservadora.
        var aliqInternaDestino = r.AliquotaDifal.HasValue
            ? r.AliquotaDifal.Value + aliqInterestadual
            : r.AliquotaIcms;

        var difal = aliqInternaDestino - aliqInterestadual;
        if (difal < 0)
        {
            difal = 0;
        }

        r.TemDifal = difal > 0;
        r.AliquotaDifal = difal;
        r.AliquotaInterestadual = aliqInterestadual;
    }
}
