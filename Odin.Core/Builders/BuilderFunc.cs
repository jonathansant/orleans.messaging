namespace Odin.Core.Builders;

/// <summary>
/// Similar to <see cref="Func{TResult}"/> but returns the same type as the argument e.g. for optional configurator.
/// supports: .WithIndices(indices => indices).
/// </summary>
/// <typeparam name="T">Type of argument/return.</typeparam>
/// <param name="arg">Argument.</param>
public delegate T BuilderFunc<T>(T arg);
