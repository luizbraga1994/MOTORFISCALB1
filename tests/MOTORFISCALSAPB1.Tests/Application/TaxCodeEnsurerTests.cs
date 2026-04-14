using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MOTORFISCALSAPB1.Application.Abstractions;
using MOTORFISCALSAPB1.Application.Services;
using MOTORFISCALSAPB1.Domain.Fiscal;
using MOTORFISCALSAPB1.Domain.Sap;
using Xunit;

namespace MOTORFISCALSAPB1.Tests.Application;

public class TaxCodeEnsurerTests
{
    private static FiscalSignature Sig(decimal icms) => new(
        "5102", "00", icms, "99", 0m, "01", 1.65m, "01", 7.6m, false, null, null);

    private static FiscalResolutionResult Result() => new()
    {
        Cfop = "5102", CstIcms = "00", AliquotaIcms = 18m,
        CstPis = "01", AliquotaPis = 1.65m,
        CstCofins = "01", AliquotaCofins = 7.6m,
        CstIpi = "99", AliquotaIpi = 0m
    };

    private sealed class NoopLock : IDistributedFiscalLock
    {
        public Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
            => Task.FromResult<IAsyncDisposable>(new Handle());
        private sealed class Handle : IAsyncDisposable { public ValueTask DisposeAsync() => default; }
    }

    [Fact]
    public async Task Retorna_mapping_existente_sem_chamar_SAP()
    {
        var sig = Sig(18m);
        var repo = new Mock<ITaxCodeMappingRepository>();
        repo.Setup(r => r.GetByHashAsync(sig.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaxCodeMapping(sig.Hash, "MF0A1B2C", sig.Canonical));
        var sap = new Mock<ISapTaxCodeService>(MockBehavior.Strict);

        var ensurer = new TaxCodeEnsurer(repo.Object, sap.Object, new NoopLock(),
            new TaxCodeCodeGenerator(), NullLogger<TaxCodeEnsurer>.Instance);

        var outcome = await ensurer.EnsureAsync(sig, Result(), CancellationToken.None);
        outcome.TaxCode.Should().Be("MF0A1B2C");
        outcome.Criado.Should().BeFalse();
        sap.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cria_via_SAP_e_persiste_quando_nao_existir()
    {
        var sig = Sig(18m);
        var repo = new Mock<ITaxCodeMappingRepository>();
        repo.Setup(r => r.GetByHashAsync(sig.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxCodeMapping?)null);

        var sap = new Mock<ISapTaxCodeService>();
        sap.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        sap.Setup(s => s.CreateAsync(It.IsAny<TaxCodeDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxCodeDefinition def, CancellationToken _) => def.Code);

        var ensurer = new TaxCodeEnsurer(repo.Object, sap.Object, new NoopLock(),
            new TaxCodeCodeGenerator(), NullLogger<TaxCodeEnsurer>.Instance);

        var outcome = await ensurer.EnsureAsync(sig, Result(), CancellationToken.None);
        outcome.Criado.Should().BeTrue();
        outcome.TaxCode.Should().StartWith("MF");
        repo.Verify(r => r.AddAsync(It.Is<TaxCodeMapping>(m => m.SignatureHash == sig.Hash), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Idempotencia_sob_concorrencia()
    {
        var sig = Sig(18m);
        var store = new Dictionary<string, TaxCodeMapping>();
        var gate = new SemaphoreSlim(1, 1);

        var repo = new Mock<ITaxCodeMappingRepository>();
        repo.Setup(r => r.GetByHashAsync(sig.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => store.TryGetValue(sig.Hash, out var m) ? m : null);
        repo.Setup(r => r.AddAsync(It.IsAny<TaxCodeMapping>(), It.IsAny<CancellationToken>()))
            .Callback<TaxCodeMapping, CancellationToken>((m, _) => store[m.SignatureHash] = m)
            .Returns(Task.CompletedTask);

        var creations = 0;
        var sap = new Mock<ISapTaxCodeService>();
        sap.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        sap.Setup(s => s.CreateAsync(It.IsAny<TaxCodeDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxCodeDefinition def, CancellationToken _) =>
            {
                Interlocked.Increment(ref creations);
                return def.Code;
            });

        var serialLock = new SerialLock(gate);
        var ensurer = new TaxCodeEnsurer(repo.Object, sap.Object, serialLock,
            new TaxCodeCodeGenerator(), NullLogger<TaxCodeEnsurer>.Instance);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => ensurer.EnsureAsync(sig, Result(), CancellationToken.None))
            .ToArray();
        var outcomes = await Task.WhenAll(tasks);

        outcomes.Select(o => o.TaxCode).Distinct().Should().HaveCount(1);
        creations.Should().Be(1);
    }

    private sealed class SerialLock : IDistributedFiscalLock
    {
        private readonly SemaphoreSlim _gate;
        public SerialLock(SemaphoreSlim gate) { _gate = gate; }
        public async Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            return new Handle(_gate);
        }
        private sealed class Handle : IAsyncDisposable
        {
            private readonly SemaphoreSlim _g;
            public Handle(SemaphoreSlim g) { _g = g; }
            public ValueTask DisposeAsync() { _g.Release(); return default; }
        }
    }
}
