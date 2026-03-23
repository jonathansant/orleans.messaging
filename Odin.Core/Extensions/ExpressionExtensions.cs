using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Odin.Core;

public static class ExpressionExtensions
{
	/// <summary>
	/// Gets member name for property within an expression.
	/// </summary>
	public static string NameOf<T, TResult>(this Expression<Func<T, TResult>> expression)
		=> NamesOf(expression).ToArray()[0];

	/// <summary>
	/// Gets a list of members within an expression
	/// </summary>
	public static IEnumerable<string> NamesOf<T, TResult>(this Expression<Func<T, TResult>> expression)
	{
		switch (expression.Body)
		{
			case MemberExpression memberExpression:
				return GetMemberName(memberExpression);
			case UnaryExpression { Operand: MemberExpression unaryMemberExpression }:
				return GetMemberName(unaryMemberExpression);
			default:
				{
					var propDict = expression.PropsPathComposite();
					return propDict == null ? [] : propDict.Keys;
				}
		}
	}

	/// <summary>
	/// Gets the member name (including complex properties). e.g. 'TransactionType.Category' => ['TransactionType', 'Category'].
	/// </summary>
	private static List<string> GetMemberName(MemberExpression? memberExpression)
	{
		var names = new List<string>();

		while (memberExpression != null)
		{
			names.Insert(0, memberExpression.Member.Name);
			memberExpression = memberExpression.Expression as MemberExpression;
		}

		return names;
	}

	/// <summary>
	/// Gets a property info within an expression.
	/// </summary>
	public static PropertyInfo Prop<T, TResult>(this Expression<Func<T, TResult>> expression)
		=> expression.Props().Single();

	/// <summary>
	/// Gets a list of property infos within an expression. <br/>
	/// <em>If the expression contains nested properties, Props will return the PropertyInfo of the last property.</em>
	///	<code>
	/// x => x.Phone -> [Phone]
	///	x => x.Phone.Number -> [Number]
	///	x => { x.Phone.Number, x.Address } -> [Number, Address]
	/// </code>
	/// </summary>
	public static IReadOnlyList<PropertyInfo> Props<T, TResult>(this Expression<Func<T, TResult>> expression)
		=> expression.MatchMemberAccessList((p, e) => e.MatchLeafMemberAccess<PropertyInfo>(p))!;

	/// <summary>
	/// Gets a list of property infos within an expression. <br/>
	/// <em>If the expression contains <b>nested properties</b>, PropsPath will return a list of all the PropertyInfo.</em><br/>
	/// <em>If the expression is a <b>composite</b> of more than one expression, PropsPath will only consider the first one
	/// (<see cref="PropsPathComposite&lt;T, TResult&gt;"/> should be used for composites).</em>
	///	<code>
	/// x => x.Phone -> [Phone]
	///	x => x.Phone.Number -> [Phone, Number]
	///	x => { x.Phone.Number, x.Address } -> [Phone, Number]
	/// </code>
	/// </summary>
	public static IReadOnlyList<PropertyInfo>? PropsPath<T, TResult>(this Expression<Func<T, TResult>> expression)
		=> PropsPathComposite(expression)?.Values.First();

	/// <summary>
	/// Gets a list of property infos keyed by the selector path.
	///	<code>
	/// x => x.Phone -> { ["Phone"] = [Phone] }
	///	x => x.Phone.Number ->  { ["Phone.Number"] = [Phone, Number] }
	///	x => { x.Phone.Number, x.Address } -> { ["Phone.Number"] = [Phone, Number], ["Address"] = [Address] }
	/// </code>
	/// </summary>
	public static IReadOnlyDictionary<string, IReadOnlyList<PropertyInfo>>? PropsPathComposite<T, TResult>(this Expression<Func<T, TResult>> expression)
	{
		var parameter = expression.Parameters[0];
		var body = expression.Body;

		if (RemoveConvert(body) is not NewExpression newExpression)
		{
			var props = parameter.MatchMemberAccess<PropertyInfo>(body);

			return props is null
				? null
				: new Dictionary<string, IReadOnlyList<PropertyInfo>> { [PathName(props)] = props };
		}

		var memberInfoDict = newExpression.Arguments
			.Select(expressionPart => parameter.MatchMemberAccess<PropertyInfo>(expressionPart))
			.Where(x => x != null)
			.ToDictionary(PathName!, propInfos => propInfos!);

		return memberInfoDict.Count != newExpression.Arguments.Count
			? null
			: memberInfoDict;

		string PathName(IEnumerable<PropertyInfo> propInfos) => propInfos.Select(x => x.Name).JoinTokens(".")!;
	}

	/// <summary>
	/// Gets the property selector accessor full path e.g. <c>x => x.Phone.Number => "Phone.Number"</c>
	/// </summary>
	public static string? GetPropertySelectorPath<TSource>(this Expression<Func<TSource, object>> expression, Func<string?, string?>? transform = null)
	{
		var memberExpression = expression.Body as MemberExpression;
		if (memberExpression == null)
			if (expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpression)
				memberExpression = unaryExpression.Operand as MemberExpression;

		var result = memberExpression?.ToString();
		result = result?[(result.IndexOf('.') + 1)..];

		return transform?.Invoke(result) ?? result;
	}

	/// <summary>
	///     <para>
	///         Returns a list of <see cref="MemberInfo" /> extracted from the given simple
	///         <see cref="LambdaExpression" />.
	///     </para>
	///     <para>
	///         Only simple expressions are supported, such as those used to reference a member.
	///     </para>
	///     <para>
	///         This method is typically used by database providers (and other extensions). It is generally
	///         not used in application code.
	///     </para>
	/// </summary>
	/// <param name="memberAccessExpression">The expression.</param>
	/// <returns>The list of referenced members.</returns>
	public static IReadOnlyList<MemberInfo> GetMemberAccessList(this LambdaExpression memberAccessExpression)
	{
		var memberPaths = memberAccessExpression
			.MatchMemberAccessList((p, e) => e.MatchSimpleMemberAccess<MemberInfo>(p));

		return memberPaths ?? throw new ArgumentException("Invalid member expression", nameof(memberAccessExpression));
	}

	public static IReadOnlyList<TMemberInfo>? MatchMemberAccessList<TMemberInfo>(
		this LambdaExpression lambdaExpression,
		Func<Expression, Expression, TMemberInfo?> memberMatcher
	) where TMemberInfo : MemberInfo
	{
		var parameterExpression = lambdaExpression.Parameters[0];

		if (RemoveConvert(lambdaExpression.Body) is NewExpression newExpression)
		{
			var expressionParts = newExpression.Arguments;
			var memberInfos = expressionParts
					.Select(expressionPart => memberMatcher(expressionPart, parameterExpression))
					.Where(p => p != null)
					.ToList();

			return memberInfos.Count != expressionParts.Count ? null : memberInfos;
		}

		var memberPath = memberMatcher(lambdaExpression.Body, parameterExpression);

		return memberPath != null ? new[] { memberPath } : null;
	}

	public static TMemberInfo? MatchSimpleMemberAccess<TMemberInfo>(this Expression parameterExpression, Expression memberAccessExpression)
		where TMemberInfo : MemberInfo
	{
		var memberInfos = MatchMemberAccess<TMemberInfo>(parameterExpression, memberAccessExpression);
		return memberInfos?.Count == 1 ? memberInfos[0] : null;
	}

	public static TMemberInfo? MatchLeafMemberAccess<TMemberInfo>(this Expression parameterExpression, Expression memberAccessExpression)
		where TMemberInfo : MemberInfo
		=> MatchMemberAccess<TMemberInfo>(parameterExpression, memberAccessExpression)?[^1];

	public static Expression? RemoveTypeAs(this Expression? expression)
	{
		while (expression?.NodeType == ExpressionType.TypeAs)
			expression = ((UnaryExpression)RemoveConvert(expression)).Operand;

		return expression;
	}

	public static Func<TEntity, TProp> GetPropertySelector<TEntity, TProp>(string propertyName)
	{
		var param = Expression.Parameter(typeof(TEntity), "x");

		var properties = propertyName.Split('.');
		var propField = properties[^1];
		MemberExpression body;

		if (properties.Length > 1)
		{
			var member = Expression.Property(param, properties[0]);

			for (var i = 1; i < properties.Length - 1; i++)
				member = Expression.Property(member, properties[i]);

			body = Expression.Property(member, propField);
		}
		else
			body = Expression.Property(param, propField);

		var convert = Expression.TypeAs(body, typeof(TProp));
		return Expression.Lambda<Func<TEntity, TProp>>(convert, param).Compile();
	}

	public static Predicate<T> ToNullCheckPredicate<T>(this Expression expression)
	{
		var sourceParam = Expression.Parameter(typeof(T), "x");
		var idPropInfo = ((MemberExpression)expression).Member as PropertyInfo;
		var nullConstant = Expression.Constant(null, idPropInfo.PropertyType);

		var nullCheckExpression = Expression.Equal(Expression.Property(sourceParam, idPropInfo), nullConstant);
		var lambdaExpression = Expression.Lambda<Predicate<T>>(nullCheckExpression, sourceParam);
		return lambdaExpression.Compile();
	}

	[return: NotNullIfNotNull("expression")]
	private static Expression? RemoveConvert(Expression? expression)
	{
		if (expression is UnaryExpression unaryExpression
			&& expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			return RemoveConvert(unaryExpression.Operand);

		return expression;
	}

	private static IReadOnlyList<TMemberInfo>? MatchMemberAccess<TMemberInfo>(
		this Expression parameterExpression,
		Expression memberAccessExpression
	) where TMemberInfo : MemberInfo
	{
		var memberInfos = new List<TMemberInfo>();

		var unwrappedExpression = RemoveTypeAs(RemoveConvert(memberAccessExpression));
		do
		{
			var memberExpression = unwrappedExpression as MemberExpression;

			if (memberExpression?.Member is not TMemberInfo memberInfo)
				return null;

			memberInfos.Insert(0, memberInfo);

			unwrappedExpression = RemoveTypeAs(RemoveConvert(memberExpression.Expression));
		}
		while (unwrappedExpression != parameterExpression);

		return memberInfos;
	}
}
