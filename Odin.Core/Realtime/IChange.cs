namespace Odin.Core.Realtime;

public interface IBifrostChange : IChange
{
	string Id { get; set; }
}

public interface IChange
{
}

public interface IClientChange : IChange
{
}

public enum ChangeType
{
	Add,
	Delete
}
