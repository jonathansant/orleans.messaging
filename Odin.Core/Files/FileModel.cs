using Humanizer;

namespace Odin.Core.Files;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record FileModel
{
	private string DebuggerDisplay => $"FileName: '{FileName}', MimeType: '{MimeType}', Content: '{Content?.Length.Bytes().Megabytes} Mb'";

	[Id(0)]
	public string FileName { get; set; }
	[Id(1)]
	public string MimeType { get; set; }
	[Id(2)]
	public byte[] Content { get; set; }

	public override string ToString() => DebuggerDisplay;
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public record FileUploadConfig
{
	protected string DebuggerDisplay => $"FileMinSizeLimitMb: {FileMinSizeLimitMb.ToDebugString()}, FileMaxSizeLimitMb: {FileMaxSizeLimitMb.ToDebugString()}, SupportedMimeTypes: {SupportedMimeTypes.ToDebugString()}";

	public double? FileMinSizeLimitMb { get; set; }
	public double? FileMaxSizeLimitMb { get; set; }
	public HashSet<string> SupportedMimeTypes { get; set; }
}
