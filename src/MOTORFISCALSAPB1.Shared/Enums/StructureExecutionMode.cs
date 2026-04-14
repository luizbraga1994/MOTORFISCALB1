namespace MOTORFISCALSAPB1.Shared.Enums;

public enum StructureExecutionMode
{
    /// <summary>
    /// Apenas valida o manifesto e reporta o que seria criado, sem executar mudanças.
    /// </summary>
    Validation = 0,

    /// <summary>
    /// Executa a criação via Service Layer.
    /// </summary>
    Execution = 1
}
