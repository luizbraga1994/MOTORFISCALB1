using MOTORFISCALSAPB1.Domain.Fiscal;

namespace MOTORFISCALSAPB1.Application.Services;

public interface ITaxCodeCodeGenerator
{
    /// <summary>
    /// Gera um código curto (≤ 8 chars) aceito pelo SAP B1 a partir da assinatura.
    /// Prefixo "MF" garante segregação de TaxCodes criados pelo motor.
    /// </summary>
    string Generate(FiscalSignature signature);
}

public sealed class TaxCodeCodeGenerator : ITaxCodeCodeGenerator
{
    public string Generate(FiscalSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        // 6 hex chars do hash -> até 16M combinações, colisão extremamente improvável
        // no universo de assinaturas distintas por empresa.
        var slice = signature.Hash.Substring(0, 6).ToUpperInvariant();
        return $"MF{slice}";
    }
}
