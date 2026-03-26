using SixLabors.Fonts;

namespace Greenshot.Maui.Core.Services;

internal static class TextFontResolver
{
	private static readonly Lazy<IReadOnlyList<FontFamily>> BundledFontFamilies = new(LoadBundledFontFamilies);
	private static readonly Lazy<IReadOnlyList<FontFamily>> ExplicitFallbackFontFamilies = new(LoadExplicitFallbackFontFamilies);
	private static readonly Dictionary<string, bool> SupportedFamilyCache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly object SupportedFamilyCacheLock = new();
	private static readonly string[] BundledFontFileNames =
	[
		"OpenSans-Regular.ttf",
		"OpenSans-Semibold.ttf"
	];
	private static readonly string[] ExplicitFallbackFontPaths =
	[
		"/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
		"/System/Library/Fonts/Supplemental/Times New Roman.ttf"
	];

	private static readonly string[] PrimaryFontCandidates =
	[
		"Arial",
		"Helvetica",
		"Helvetica Neue",
		"Segoe UI",
		"Open Sans",
		"Noto Sans",
		"DejaVu Sans",
		"Liberation Sans"
	];

	private static readonly string[] FallbackFontCandidates =
	[
		"Open Sans",
		"Arial",
		"Helvetica",
		"Helvetica Neue",
		"Segoe UI",
		"Hiragino Sans",
		"Hiragino Kaku Gothic ProN",
		"PingFang SC",
		"PingFang TC",
		"PingFang HK",
		"Apple SD Gothic Neo",
		"Yu Gothic",
		"Meiryo",
		"Microsoft YaHei",
		"Microsoft JhengHei",
		"Malgun Gothic",
		"Noto Sans CJK JP",
		"Noto Sans CJK SC",
		"Noto Sans CJK TC",
		"Noto Sans CJK KR",
		"Arial Unicode MS",
		"Noto Sans",
		"DejaVu Sans",
		"Liberation Sans"
	];

	public static ResolvedTextFont Resolve(float fontSize)
	{
		var bundledFamilies = BundledFontFamilies.Value;
		var explicitFallbackFamilies = ExplicitFallbackFontFamilies.Value;
		var primaryFamily = ResolvePrimaryFamily(SystemFonts.Collection, bundledFamilies, explicitFallbackFamilies);
		var fallbackFamilies = ResolveFallbackFamilies(SystemFonts.Collection, primaryFamily, bundledFamilies, explicitFallbackFamilies);

		return new ResolvedTextFont(
			primaryFamily.CreateFont(fontSize, FontStyle.Regular),
			fallbackFamilies);
	}

	internal static FontFamily ResolvePrimaryFamily(
		IReadOnlyFontCollection fontCollection,
		IReadOnlyList<FontFamily>? bundledFamilies = null,
		IReadOnlyList<FontFamily>? explicitFallbackFamilies = null)
	{
		var preferredFamily = FirstOrNone(ResolveFontFamilies(fontCollection, PrimaryFontCandidates));
		if (preferredFamily.HasValue)
		{
			return preferredFamily.Value;
		}

		var systemFallbackFamily = FirstOrNone(fontCollection.Families.Where(CanUseFamily));
		if (systemFallbackFamily.HasValue)
		{
			return systemFallbackFamily.Value;
		}

		var bundledFallbackFamily = FirstOrNone(bundledFamilies);
		if (bundledFallbackFamily.HasValue)
		{
			return bundledFallbackFamily.Value;
		}

		var explicitFallbackFamily = FirstOrNone(explicitFallbackFamilies);
		if (explicitFallbackFamily.HasValue)
		{
			return explicitFallbackFamily.Value;
		}

		throw new InvalidOperationException("No fonts are available for text rendering.");
	}

	internal static IReadOnlyList<FontFamily> ResolveFallbackFamilies(
		IReadOnlyFontCollection fontCollection,
		FontFamily primaryFamily,
		IReadOnlyList<FontFamily>? bundledFamilies = null,
		IReadOnlyList<FontFamily>? explicitFallbackFamilies = null)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			primaryFamily.Name
		};
		var fallbackFamilies = new List<FontFamily>();

		foreach (var family in explicitFallbackFamilies ?? Array.Empty<FontFamily>())
		{
			if (CanUseFamily(family) && seen.Add(family.Name))
			{
				fallbackFamilies.Add(family);
			}
		}

		foreach (var family in bundledFamilies ?? Array.Empty<FontFamily>())
		{
			if (CanUseFamily(family) && seen.Add(family.Name))
			{
				fallbackFamilies.Add(family);
			}
		}

		foreach (var family in ResolveFontFamilies(fontCollection, FallbackFontCandidates))
		{
			if (seen.Add(family.Name))
			{
				fallbackFamilies.Add(family);
			}
		}

		if (fallbackFamilies.Count > 0)
		{
			return fallbackFamilies;
		}

		var firstNonPrimaryFamily = FirstOrNone(fontCollection.Families.Where(family => CanUseFamily(family) && seen.Add(family.Name)));
		if (firstNonPrimaryFamily.HasValue)
		{
			return [firstNonPrimaryFamily.Value];
		}

		var explicitNonPrimaryFamily = FirstOrNone((explicitFallbackFamilies ?? Array.Empty<FontFamily>()).Where(family => CanUseFamily(family) && seen.Add(family.Name)));
		if (explicitNonPrimaryFamily.HasValue)
		{
			return [explicitNonPrimaryFamily.Value];
		}

		var bundledNonPrimaryFamily = FirstOrNone((bundledFamilies ?? Array.Empty<FontFamily>()).Where(family => CanUseFamily(family) && seen.Add(family.Name)));
		return !bundledNonPrimaryFamily.HasValue
			? Array.Empty<FontFamily>()
			: [bundledNonPrimaryFamily.Value];
	}

	internal static IReadOnlyList<FontFamily> ResolveFontFamilies(
		IReadOnlyFontCollection fontCollection,
		IEnumerable<string> candidateNames)
	{
		var families = new List<FontFamily>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var candidateName in candidateNames)
		{
			if (!seen.Add(candidateName))
			{
				continue;
			}

			if (fontCollection.TryGet(candidateName, out var family) && CanUseFamily(family))
			{
				families.Add(family);
			}
		}

		return families;
	}

	internal static IReadOnlyList<FontFamily> LoadFontFamilies(IEnumerable<string> fontPaths)
	{
		var fontCollection = new FontCollection();
		var families = new List<FontFamily>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var fontPath in fontPaths)
		{
			if (string.IsNullOrWhiteSpace(fontPath) || !File.Exists(fontPath))
			{
				continue;
			}

			try
			{
				var family = fontCollection.Add(fontPath);
				if (CanUseFamily(family) && seen.Add(family.Name))
				{
					families.Add(family);
				}
			}
			catch (Exception)
			{
				// Ignore invalid font payloads and continue to the next candidate.
			}
		}

		return families;
	}

	internal static IReadOnlyList<string> EnumerateBundledFontPaths(string baseDirectory)
	{
		var paths = new List<string>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var candidateRoot in EnumerateCandidateDirectories(baseDirectory))
		{
			foreach (var fileName in BundledFontFileNames)
			{
				foreach (var candidatePath in EnumerateBundledFontPathsForRoot(candidateRoot, fileName))
				{
					var fullPath = Path.GetFullPath(candidatePath);
					if (File.Exists(fullPath) && seen.Add(fullPath))
					{
						paths.Add(fullPath);
					}
				}
			}
		}

		return paths;
	}

	private static FontFamily? FirstOrNone(IEnumerable<FontFamily>? families)
	{
		if (families is null)
		{
			return null;
		}

		var family = families.FirstOrDefault();
		return family.Equals(default(FontFamily))
			? null
			: family;
	}

	private static IReadOnlyList<FontFamily> LoadBundledFontFamilies() =>
		LoadFontFamilies(EnumerateBundledFontPaths(AppContext.BaseDirectory));

	private static IReadOnlyList<FontFamily> LoadExplicitFallbackFontFamilies() =>
		LoadFontFamilies(ExplicitFallbackFontPaths);

	private static IEnumerable<string> EnumerateCandidateDirectories(string? baseDirectory)
	{
		if (string.IsNullOrWhiteSpace(baseDirectory))
		{
			yield break;
		}

		var currentDirectory = Path.GetFullPath(baseDirectory);
		for (var depth = 0; depth < 10; depth++)
		{
			yield return currentDirectory;

			var parentDirectory = Directory.GetParent(currentDirectory);
			if (parentDirectory is null)
			{
				yield break;
			}

			currentDirectory = parentDirectory.FullName;
		}
	}

	private static IEnumerable<string> EnumerateBundledFontPathsForRoot(string candidateRoot, string fileName)
	{
		yield return Path.Combine(candidateRoot, fileName);
		yield return Path.Combine(candidateRoot, "Resources", fileName);
		yield return Path.Combine(candidateRoot, "Fonts", fileName);
		yield return Path.Combine(candidateRoot, "Resources", "Fonts", fileName);
		yield return Path.Combine(candidateRoot, "Contents", "Resources", fileName);
		yield return Path.Combine(candidateRoot, "Greenshot.app", "Contents", "Resources", fileName);
		yield return Path.Combine(candidateRoot, "src", "Greenshot.Maui", "Resources", "Fonts", fileName);
	}

	private static bool CanUseFamily(FontFamily family)
	{
		if (family.Equals(default(FontFamily)))
		{
			return false;
		}

		lock (SupportedFamilyCacheLock)
		{
			if (SupportedFamilyCache.TryGetValue(family.Name, out var isSupported))
			{
				return isSupported;
			}
		}

		var canUseFamily = ProbeFamily(family);

		lock (SupportedFamilyCacheLock)
		{
			SupportedFamilyCache[family.Name] = canUseFamily;
		}

		return canUseFamily;
	}

	private static bool ProbeFamily(FontFamily family)
	{
		try
		{
			var probeFont = family.CreateFont(12f, FontStyle.Regular);
			_ = TextMeasurer.MeasureBounds("Ag", new TextOptions(probeFont));
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}

internal readonly record struct ResolvedTextFont(
	Font Font,
	IReadOnlyList<FontFamily> FallbackFontFamilies);
