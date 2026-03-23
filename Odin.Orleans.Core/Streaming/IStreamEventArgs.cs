namespace Odin.Orleans.Core.Streaming;

public interface IStreamEventArgs
{
	string Id { get; set; }
}

public interface IStreamEventArgs<T> : IStreamEventArgs
{
	T Data { get; set; }
}
