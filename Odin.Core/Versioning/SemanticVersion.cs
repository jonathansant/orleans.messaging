using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Odin.Core.Versioning;

// ref: https://github.com/NuGet/NuGet2/blob/2.13/src/Core/SemanticVersion.cs
/// <summary>
/// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to
/// allow older 4-digit versioning schemes to continue working.
/// </summary>
[Serializable]
[TypeConverter(typeof(SemanticVersionTypeConverter))]
public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
	private const RegexOptions Flags = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
	private static readonly Regex SemanticVersionRegex = new Regex(@"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[a-z][0-9a-z-]*)?$", Flags);
	private static readonly Regex StrictSemanticVersionRegex = new Regex(@"^(?<Version>\d+(\.\d+){2})(?<Release>-[a-z][0-9a-z-]*)?$", Flags);
	private readonly string _originalString;
	private string _normalizedVersionString;

	/// <summary>
	/// Gets the normalized version portion.
	/// </summary>
	public Version Version { get; }

	/// <summary>
	/// Gets the optional special version.
	/// </summary>
	public string SpecialVersion { get; }

	public SemanticVersion(string version)
		: this(Parse(version))
	{
		// The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it.
		// The original string represents the original form in which the version is represented to be used when printing.
		_originalString = version;
	}

	public SemanticVersion(int major, int minor, int build, int revision)
		: this(new Version(major, minor, build, revision))
	{
	}

	public SemanticVersion(int major, int minor, int build, string specialVersion)
		: this(new Version(major, minor, build), specialVersion)
	{
	}

	public SemanticVersion(Version version)
		: this(version, string.Empty)
	{
	}

	public SemanticVersion(Version version, string specialVersion)
		: this(version, specialVersion, null)
	{
	}

	private SemanticVersion(Version version, string? specialVersion, string originalString)
	{
		if (version == null)
		{
			throw new ArgumentNullException(nameof(version));
		}
		Version = NormalizeVersionValue(version);
		SpecialVersion = specialVersion ?? string.Empty;
		_originalString = string.IsNullOrEmpty(originalString)
			? version + (!string.IsNullOrEmpty(specialVersion)
				? '-' + specialVersion : null)
			: originalString;
	}

	internal SemanticVersion(SemanticVersion semVer)
	{
		_originalString = semVer.ToString();
		Version = semVer.Version;
		SpecialVersion = semVer.SpecialVersion;
	}

	public string[] GetOriginalVersionComponents()
	{
		if (string.IsNullOrEmpty(_originalString))
			return SplitAndPadVersionString(Version.ToString());

		// search the start of the SpecialVersion part, if any
		var dashIndex = _originalString.IndexOf('-');
		var original = dashIndex != -1
			? _originalString.Substring(0, dashIndex)
			: _originalString;

		return SplitAndPadVersionString(original);
	}

	private static string[] SplitAndPadVersionString(string version)
	{
		var a = version.Split('.');
		if (a.Length == 4)
			return a;

		// if 'a' has less than 4 elements, we pad the '0' at the end
		// to make it 4.
		var b = new[] { "0", "0", "0", "0" };
		Array.Copy(a, 0, b, 0, a.Length);
		return b;
	}

	/// <summary>
	/// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
	/// </summary>
	public static SemanticVersion Parse(string version)
	{
		if (version.IsNullOrEmpty())
			throw new ArgumentException("Version must be defined", nameof(version));

		if (!TryParse(version, out var semVer))
			throw new ArgumentException($"'{version}' is not a valid version string.", nameof(version));
		return semVer;
	}

	/// <summary>
	/// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
	/// </summary>
	public static bool TryParse(string version, out SemanticVersion value)
		=> TryParseInternal(version, SemanticVersionRegex, out value);

	/// <summary>
	/// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
	/// </summary>
	public static bool TryParseStrict(string version, out SemanticVersion value)
		=> TryParseInternal(version, StrictSemanticVersionRegex, out value);

	private static bool TryParseInternal(string version, Regex regex, out SemanticVersion semVer)
	{
		semVer = null;
		if (string.IsNullOrEmpty(version))
			return false;

		var match = regex.Match(version.Trim());
		if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out var versionValue))
			return false;

		semVer = new SemanticVersion(NormalizeVersionValue(versionValue), match.Groups["Release"].Value.TrimStart('-'), version.Replace(" ", ""));
		return true;
	}

	/// <summary>
	/// Attempts to parse the version token as a SemanticVersion.
	/// </summary>
	/// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
	public static SemanticVersion ParseOptionalVersion(string version)
	{
		TryParse(version, out var semVer);
		return semVer;
	}

	private static Version NormalizeVersionValue(Version version)
		=> new Version(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));

	public int CompareTo(object obj)
	{
		if (ReferenceEquals(obj, null))
			return 1;
		var other = obj as SemanticVersion ?? throw new ArgumentException("Type to compare must be an instance of SemanticVersion.", nameof(obj));
		return CompareTo(other);
	}

	public int CompareTo(SemanticVersion other)
	{
		if (ReferenceEquals(other, null))
			return 1;

		var result = Version.CompareTo(other.Version);

		if (result != 0)
			return result;

		var isEmpty = string.IsNullOrEmpty(SpecialVersion);
		var isOtherEmpty = string.IsNullOrEmpty(other.SpecialVersion);
		if (isEmpty && isOtherEmpty)
			return 0;

		if (isEmpty)
			return 1;

		if (isOtherEmpty)
			return -1;
		return StringComparer.OrdinalIgnoreCase.Compare(SpecialVersion, other.SpecialVersion);
	}

	public static bool operator ==(SemanticVersion? version1, SemanticVersion version2)
		=> version1?.Equals(version2) ?? ReferenceEquals(version2, null);

	public static bool operator !=(SemanticVersion version1, SemanticVersion version2)
		=> !(version1 == version2);

	public static bool operator <(SemanticVersion version1, SemanticVersion version2)
	{
		if (version1 == null)
			throw new ArgumentNullException(nameof(version1));
		return version1.CompareTo(version2) < 0;
	}

	public static bool operator <=(SemanticVersion version1, SemanticVersion version2)
		=> version1 == version2 || version1 < version2;

	public static bool operator >(SemanticVersion version1, SemanticVersion version2)
	{
		if (version1 == null)
			throw new ArgumentNullException(nameof(version1));
		return version2 < version1;
	}

	public static bool operator >=(SemanticVersion version1, SemanticVersion version2)
		=> version1 == version2 || version1 > version2;

	public override string ToString()
		=> _originalString;

	/// <summary>
	/// Returns the normalized string representation of this instance of <see cref="SemanticVersion"/>.
	/// If the instance can be strictly parsed as a <see cref="SemanticVersion"/>, the normalized version
	/// string if of the format {major}.{minor}.{build}[-{special-version}]. If the instance has a non-zero
	/// value for <see cref="Version.Revision"/>, the format is {major}.{minor}.{build}.{revision}[-{special-version}].
	/// </summary>
	/// <returns>The normalized string representation.</returns>
	public string ToNormalizedString()
	{
		if (_normalizedVersionString != null)
			return _normalizedVersionString;

		var builder = new StringBuilder();
		builder
			.Append(Version.Major)
			.Append('.')
			.Append(Version.Minor)
			.Append('.')
			.Append(Math.Max(0, Version.Build));

		if (Version.Revision > 0)
		{
			builder.Append('.')
				.Append(Version.Revision);
		}

		if (!string.IsNullOrEmpty(SpecialVersion))
		{
			builder.Append('-')
				.Append(SpecialVersion);
		}

		_normalizedVersionString = builder.ToString();

		return _normalizedVersionString;
	}

	public bool Equals(SemanticVersion other)
		=> !ReferenceEquals(null, other)
		   && Version.Equals(other.Version)
		   && SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);

	public override bool Equals(object obj)
		=> obj is SemanticVersion semVer && Equals(semVer);

	public override int GetHashCode()
	{
		var hashCode = Version.GetHashCode();
		if (SpecialVersion != null)
			hashCode = hashCode * 4567 + SpecialVersion.GetHashCode();

		return hashCode;
	}
}
