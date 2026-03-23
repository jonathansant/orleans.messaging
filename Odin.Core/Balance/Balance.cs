using Odin.Core.Money;

namespace Odin.Core.Balance;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record Balance : MoneyModel { }
