using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DistinguishedService;

// Compatibility helpers for Bannerlord 1.4.x API changes.
internal static class DSCompat
{
	// CultureObject.NotableAndWandererTemplates was removed (NotableTemplates now holds only notables).
	// Look wanderers up directly by culture + occupation instead.
	public static CharacterObject GetRandomWanderer(CultureObject culture, Func<CharacterObject, bool> predicate)
	{
		List<CharacterObject> list = CharacterObject.All
			.Where((CharacterObject c) => c.Occupation == Occupation.Wanderer && c.Culture == culture && predicate(c))
			.ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return list[MBRandom.RandomInt(list.Count)];
	}

	// CultureCode / GetCultureCode were removed; culture StringId is the stable identifier.
	public static string CultureKey(CultureObject culture)
	{
		return culture?.StringId ?? "";
	}
}
