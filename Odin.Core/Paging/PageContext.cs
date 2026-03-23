namespace Odin.Core.Paging;

public interface IPageContext
{
	int Current { get; }
	int TotalItems { get; }
	int Size { get; }
	int TotalPages { get; }
}

[GenerateSerializer, Immutable]
public readonly record struct PageContext<T>() : IPageContext
{
	[Id(0)]
	public List<T> Data { get; init; } = new();
	[Id(1)]
	public int Current { get; init; } = 0;
	[Id(2)]
	public int TotalItems { get; init; } = 0;
	[Id(3)]
	public int Size { get; init; } = 0;
	[Id(4)]
	public int TotalPages { get; init; } = 0;


	public static readonly PageContext<T> Empty = new();

	public static PageContext<T> GetEmpty(int size = 0, int current = 0)
		=> new() { TotalItems = 0, Current = current, Data = new(), Size = size };

	public static PageContext<T> Create(List<T> data, int current, int totalItems, int pageSize)
		=> new()
		{
			TotalItems = totalItems,
			Current = current,
			Data = data,
			Size = data.Count,
			TotalPages = totalItems.DivideAndCeil(pageSize)
		};

	public static PageContext<T> Create<TOther>(PageContext<TOther> page, List<T> data)
		=> new()
		{
			TotalItems = page.TotalItems,
			Current = page.Current,
			Data = data,
			Size = page.Data.Count,
			TotalPages = page.TotalPages
		};

	public static PageContext<T> Create<TOther>(PageContext<TOther> page, Func<List<TOther>, List<T>> transform)
		=> Create(page, transform(page.Data));
}

public static class PageContext
{
	public static PageContext<TResult> Create<T, TResult>(PageContext<T> page, List<TResult> data)
		=> PageContext<TResult>.Create(page, data);

	public static PageContext<TResult> Create<T, TResult>(PageContext<T> page, Func<List<T>, List<TResult>> transform)
		=> PageContext<TResult>.Create(page, transform);

	public static PageContext<T> Create<T>(List<T> data, int current, int totalItems, int pageSize)
		=> PageContext<T>.Create(data, current, totalItems, pageSize);
}

public static class PageContextExtensions
{
	public static PageContext<T> Transform<T>(this PageContext<T> pageContext, Func<T, T> transformFunc)
		=> PageContext<T>.Create(pageContext, pageContext.Data.Select(transformFunc).ToList());

	/// <summary>
	/// Compares every property except the <see cref="PageContext&lt;TOther&gt;.Data"/>.
	/// </summary>
	public static bool EqualPages<TCurrent, TOther>(this PageContext<TCurrent> pageContext, PageContext<TOther> other)
		=> pageContext.Current == other.Current &&
		   pageContext.TotalItems == other.TotalItems &&
		   pageContext.Size == other.Size &&
		   pageContext.TotalPages == other.TotalPages;
}
