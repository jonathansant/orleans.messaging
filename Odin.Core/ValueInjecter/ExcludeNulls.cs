using Odin.Core.ValueInjecter;
using Omu.ValueInjecter.Injections;
using System.Collections;
using System.Reflection;

namespace Odin.Core.ValueInjecter
{
	public class ExcludeNulls : LoopInjection
	{
		protected override void SetValue(object source, object target, PropertyInfo sp, PropertyInfo tp)
		{
			if (sp.GetValue(source) == null) return;
			base.SetValue(source, target, sp, tp);
		}
	}
}

namespace Omu.ValueInjecter
{
	public static class ValueInjecterExtensions
	{
		public static object InjectFromExcludingNulls(this object target, object source)
		{
			if (target is IDictionary targetDictionary)
				return Result(target, source, targetDictionary);

			return source == null ? target : target?.InjectFrom<ExcludeNulls>(source);
		}

		public static TResult InjectFromExcludingNulls<TResult>(this TResult target, object source)
		{
			if (target is IDictionary targetDictionary)
				return Result(target, source, targetDictionary);

			return (TResult)(source == null ? target : target?.InjectFrom<ExcludeNulls>(source));
		}

		private static TResult Result<TResult>(TResult target, object source, IDictionary targetDictionary)
		{
			foreach (DictionaryEntry sourceItem in (IDictionary)source)
			{
				if (!targetDictionary.Contains(sourceItem.Key))
					continue;

				var item = targetDictionary[sourceItem.Key];

				switch (item)
				{
					case string _:
					case ValueType _:
						targetDictionary[sourceItem.Key] = sourceItem.Value;
						break;
					default:
						item.InjectFrom<ExcludeNulls>(sourceItem.Value);
						break;
				}
			}

			return target;
		}
	}
}
