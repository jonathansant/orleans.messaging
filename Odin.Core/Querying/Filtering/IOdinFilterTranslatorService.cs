namespace Odin.Core.Querying.Filtering;

public interface IOdinFilterExpressionTranslator<TQueryBody>
{
	ValueTask<TQueryBody> Translate(OdinFilterInput filter, TQueryBody queryBody);
}

// todo: need to handle multiple services registered at the same time.
public interface IOdinFilterTranslatorService<TQueryBody> : IOdinFilterExpressionTranslator<TQueryBody>
{
	string And { get; }
	string Or { get; }
	Dictionary<FilterOperator, string> FilterOperators { get; }
}
