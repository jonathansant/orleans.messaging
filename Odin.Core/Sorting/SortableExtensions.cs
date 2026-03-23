namespace Odin.Core.Sorting;

public interface ISortable
{
	int? SortOrder { get; set; }
}

/// <summary>
/// Extension members for <see cref="ISortable"/> collections and items.
/// </summary>
public static class SortableExtensions
{
	extension<T>(IEnumerable<T> items) where T : class, ISortable
	{
		/// <summary>
		/// Assigns sequential sort orders to a list of items.
		/// </summary>
		/// <param name="startingSortOrder">The first sort order value to assign.</param>
		/// <param name="force">
		/// When <c>true</c>, always overwrites existing sort orders.
		/// When <c>false</c>, only assigns if <see cref="ISortable.SortOrder"/> is <c>null</c>.
		/// </param>
		public void AssignSortOrder(int startingSortOrder = 1, bool force = true)
		{
			ArgumentNullException.ThrowIfNull(items);

			var current = startingSortOrder;
			var list = items.ToList();
			foreach (var item in list)
			{
				if (force)
					item.SortOrder = current++;
				else
					item.SortOrder ??= current++;
			}

			list.ToSorted();
		}

		/// <summary>
		/// Returns a new list of items ordered by <see cref="ISortable.SortOrder"/>.
		/// Items with <c>null</c> sort orders are placed at the end.
		/// </summary>
		/// <returns>A new sorted list.</returns>
		public List<T> ToSorted()
		{
			ArgumentNullException.ThrowIfNull(items);
			return items.OrderBy(x => x.SortOrder ?? int.MaxValue).ToList();
		}
	}

	extension<T>(List<T> allItems) where T : class, ISortable
	{
		/// <summary>
		/// Repositions a single item within a list by shifting only the items whose sort orders
		/// fall between the item's previous and new positions. Items outside this range are untouched.
		/// Uses reference equality (<see cref="object.ReferenceEquals"/>) to identify <paramref name="item"/>
		/// within <paramref name="allItems"/>, so <paramref name="item"/> must be the same object instance
		/// that already exists in the list.
		/// <list type="bullet">
		///   <item>Moving <b>up</b> (newSortOrder &lt; previousSortOrder): items in <c>[newSortOrder, previousSortOrder)</c> shift down by 1.</item>
		///   <item>Moving <b>down</b> (newSortOrder &gt; previousSortOrder): items in <c>(previousSortOrder, newSortOrder]</c> shift up by 1.</item>
		/// </list>
		/// </summary>
		/// <param name="item">The item being repositioned. Its <see cref="ISortable.SortOrder"/> must already reflect the <b>new</b> position.</param>
		/// <param name="previousSortOrder">The sort order the item held before being repositioned.</param>
		/// <exception cref="InvalidOperationException">Thrown when the item's <see cref="ISortable.SortOrder"/> is <c>null</c>.</exception>
		public void ShiftSortOrder(T item, int previousSortOrder)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(allItems);

			if (item.SortOrder is not { } newSortOrder)
				throw new InvalidOperationException("The item's SortOrder must be set before calling ShiftSortOrder.");

			if (newSortOrder == previousSortOrder)
				return;

			if (newSortOrder < previousSortOrder)
			{
				// Moving up: items in [newSortOrder, previousSortOrder) shift down by 1
				foreach (var i in allItems.Where(i => !ReferenceEquals(i, item) && i.SortOrder >= newSortOrder && i.SortOrder < previousSortOrder).ToList())
					i.SortOrder++;
			}
			else
			{
				// Moving down: items in (previousSortOrder, newSortOrder] shift up by 1
				foreach (var i in allItems.Where(i => !ReferenceEquals(i, item) && i.SortOrder > previousSortOrder && i.SortOrder <= newSortOrder).ToList())
					i.SortOrder--;
			}

			allItems.ToSorted();
		}

		/// <summary>
		/// Repositions a single item within a list by shifting only the items whose sort orders
		/// fall between the item's previous and new positions. Items outside this range are untouched.
		/// <list type="bullet">
		///   <item>Moving <b>up</b> (newSortOrder &lt; previousSortOrder): items in <c>[newSortOrder, previousSortOrder)</c> shift down by 1.</item>
		///   <item>Moving <b>down</b> (newSortOrder &gt; previousSortOrder): items in <c>(previousSortOrder, newSortOrder]</c> shift up by 1.</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TKey">The key type used to identify the repositioned item.</typeparam>
		/// <param name="item">The item being repositioned. Its <see cref="ISortable.SortOrder"/> must already reflect the <b>new</b> position.</param>
		/// <param name="previousSortOrder">The sort order the item held before being repositioned.</param>
		/// <param name="keySelector">A function that extracts a unique key from each item for identity comparison.</param>
		/// <exception cref="InvalidOperationException">Thrown when the item's <see cref="ISortable.SortOrder"/> is <c>null</c>.</exception>
		public void ShiftSortOrder<TKey>(T item, int previousSortOrder, Func<T, TKey> keySelector)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(allItems);
			ArgumentNullException.ThrowIfNull(keySelector);

			if (item.SortOrder is not { } newSortOrder)
				throw new InvalidOperationException("The item's SortOrder must be set before calling ShiftSortOrder.");

			if (newSortOrder == previousSortOrder)
				return;

			var itemKey = keySelector(item);
			var comparer = EqualityComparer<TKey>.Default;

			if (newSortOrder < previousSortOrder)
			{
				// Moving up: items in [newSortOrder, previousSortOrder) shift down by 1
				var itemsToShift = allItems
					.Where(i => !comparer.Equals(keySelector(i), itemKey)
						&& i.SortOrder >= newSortOrder
						&& i.SortOrder < previousSortOrder)
					.ToList();

				foreach (var toShift in itemsToShift)
					toShift.SortOrder++;
			}
			else
			{
				// Moving down: items in (previousSortOrder, newSortOrder] shift up by 1
				var itemsToShift = allItems
					.Where(i => !comparer.Equals(keySelector(i), itemKey)
						&& i.SortOrder > previousSortOrder
						&& i.SortOrder <= newSortOrder)
					.ToList();

				foreach (var toShift in itemsToShift)
					toShift.SortOrder--;
			}
		}

		/// <summary>
		/// Adds a new item into a list at its designated sort position by shifting all existing items
		/// whose sort order is at or after the new item's position down by 1 to make room.
		/// The new item must already be added to <paramref name="allItems"/> before calling this method.
		/// Uses reference equality (<see cref="object.ReferenceEquals"/>) to identify <paramref name="item"/>
		/// within <paramref name="allItems"/>, so <paramref name="item"/> must be the same object instance
		/// that was added to the list.
		/// </summary>
		/// <param name="item">The new item being inserted. Its <see cref="ISortable.SortOrder"/> must already be set to the desired position,
		/// or <c>null</c> to automatically place it at the end of the list.</param>
		/// <remarks>
		/// If <see cref="ISortable.SortOrder"/> is <c>null</c>, the item is placed after the highest existing sort order
		/// and no other items are shifted.
		/// </remarks>
		public void AddSortOrder(T item)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(allItems);

			// If no sort order is set, place the item at the end
			if (item.SortOrder is null)
			{
				var max = allItems
					.Where(i => !ReferenceEquals(i, item) && i.SortOrder.HasValue)
					.Select(i => i.SortOrder!.Value)
					.DefaultIfEmpty(0)
					.Max();
				item.SortOrder = max + 1;
				return; // nothing else to shift — item is already at the end
			}

			var newSortOrder = item.SortOrder.Value;

			// Shift all existing items at >= newSortOrder down by 1 to make room
			foreach (var i in allItems.Where(i => !ReferenceEquals(i, item) && i.SortOrder >= newSortOrder).ToList())
				i.SortOrder++;
		}

		/// <summary>
		/// Adds a new item into a list at its designated sort position by shifting all existing items
		/// whose sort order is at or after the new item's position down by 1 to make room.
		/// The new item must already be added to <paramref name="allItems"/> before calling this method.
		/// </summary>
		/// <typeparam name="TKey">The key type used to identify the new item.</typeparam>
		/// <param name="item">The new item being inserted. Its <see cref="ISortable.SortOrder"/> must already be set to the desired position,
		/// or <c>null</c> to automatically place it at the end of the list.</param>
		/// <param name="keySelector">A function that extracts a unique key from each item for identity comparison.</param>
		/// <remarks>
		/// If <see cref="ISortable.SortOrder"/> is <c>null</c>, the item is placed after the highest existing sort order
		/// and no other items are shifted.
		/// </remarks>
		public void AddSortOrder<TKey>(T item, Func<T, TKey> keySelector)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(allItems);
			ArgumentNullException.ThrowIfNull(keySelector);

			var itemKey = keySelector(item);
			var comparer = EqualityComparer<TKey>.Default;

			// If no sort order is set, place the item at the end
			if (item.SortOrder is null)
			{
				var maxSortOrder = allItems
					.Where(i => !comparer.Equals(keySelector(i), itemKey) && i.SortOrder.HasValue)
					.Select(i => i.SortOrder!.Value)
					.DefaultIfEmpty(0)
					.Max();
				item.SortOrder = maxSortOrder + 1;
				return; // nothing else to shift — item is already at the end
			}

			var newSortOrder = item.SortOrder.Value;

			// Shift all existing items at >= newSortOrder down by 1 to make room
			var itemsToShift = allItems
				.Where(i => !comparer.Equals(keySelector(i), itemKey) && i.SortOrder >= newSortOrder)
				.ToList();

			foreach (var toShift in itemsToShift)
				toShift.SortOrder++;
		}

		/// <summary>
		/// Adds a range of new items into the list at their designated sort positions.
		/// Each item is added to <paramref name="allItems"/> and then inserted individually
		/// via <see cref="AddSortOrder"/>, so items with a non-<c>null</c> <see cref="ISortable.SortOrder"/>
		/// will shift existing items to make room, and items with a <c>null</c> sort order are placed at the end.
		/// The new items must <b>not</b> already be in <paramref name="allItems"/>; this method adds them.
		/// Uses reference equality (<see cref="object.ReferenceEquals"/>) to identify each item.
		/// </summary>
		/// <param name="newItems">The new items to insert. Items with a set sort order are processed first
		/// (in ascending order), then items with <c>null</c> sort orders are appended at the end.</param>
		public void AddRangeSortOrder(IEnumerable<T> newItems)
		{
			ArgumentNullException.ThrowIfNull(allItems);
			ArgumentNullException.ThrowIfNull(newItems);

			// Process items with explicit sort orders first (ascending), then nulls
			var ordered = newItems
				.OrderBy(x => x.SortOrder.HasValue ? 0 : 1)
				.ThenBy(x => x.SortOrder ?? int.MaxValue)
				.ToList();

			foreach (var item in ordered)
			{
				allItems.Add(item);
				allItems.AddSortOrder(item);
			}
		}

		/// <summary>
		/// Adds a range of new items into the list at their designated sort positions.
		/// Each item is added to <paramref name="allItems"/> and then inserted individually
		/// via <see cref="AddSortOrder{TKey}"/>, so items with a non-<c>null</c> <see cref="ISortable.SortOrder"/>
		/// will shift existing items to make room, and items with a <c>null</c> sort order are placed at the end.
		/// The new items must <b>not</b> already be in <paramref name="allItems"/>; this method adds them.
		/// </summary>
		/// <typeparam name="TKey">The key type used to identify each item.</typeparam>
		/// <param name="newItems">The new items to insert. Items with a set sort order are processed first
		/// (in ascending order), then items with <c>null</c> sort orders are appended at the end.</param>
		/// <param name="keySelector">A function that extracts a unique key from each item for identity comparison.</param>
		public void AddRangeSortOrder<TKey>(IEnumerable<T> newItems, Func<T, TKey> keySelector)
		{
			ArgumentNullException.ThrowIfNull(allItems);
			ArgumentNullException.ThrowIfNull(newItems);
			ArgumentNullException.ThrowIfNull(keySelector);

			// Process items with explicit sort orders first (ascending), then nulls
			var ordered = newItems
				.OrderBy(x => x.SortOrder.HasValue ? 0 : 1)
				.ThenBy(x => x.SortOrder ?? int.MaxValue)
				.ToList();

			foreach (var item in ordered)
			{
				allItems.Add(item);
				allItems.AddSortOrder(item, keySelector);
			}
		}

		/// <summary>
		/// Re-normalizes sort orders so they are sequential starting from 1,
		/// based on the current ordering. Items with <c>null</c> sort orders are placed at the end.
		/// Modifies <see cref="ISortable.SortOrder"/> in-place.
		/// </summary>
		public void NormalizeSortOrder()
		{
			ArgumentNullException.ThrowIfNull(allItems);

			var i = 1;
			foreach (var item in allItems.OrderBy(x => x.SortOrder ?? int.MaxValue))
				item.SortOrder = i++;
		}
	}
}