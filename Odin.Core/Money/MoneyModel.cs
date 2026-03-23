namespace Odin.Core.Money;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record MoneyModel
{
	public string DebuggerDisplay => $"Amount: {Amount}, Currency: '{Currency}'";

	[Id(0)]
	public decimal Amount { get; set; }
	[Id(1)]
	public string Currency { get; set; }

	public override string ToString() => DebuggerDisplay;
}
