namespace StringExtensions;
public static class StringExtensions {
	/// <summary>
	/// Substring that doesn't throw ArgumentOutOfRangeException and silently return empty string. Could be simplified with Range indexing in C# 10 if we upgrade one day
	/// </summary>
	/// <param name="s">The string s.</param>
	/// <param name="startIndex">The start index.</param>
	/// <returns></returns>
	public static string SubstringSafe(this string s, int startIndex) {
		if (string.IsNullOrEmpty(s))  {
			return s;
		}
		if (startIndex > s.Length) {
			return string.Empty;    //don't throw ArgumentOutOfRangeException
		}
		if (startIndex < 0) {
			return s[Math.Max(0, s.Length + startIndex)..];  //Index from the right. Adding startIndex will actually subtract since it's negative
		}
		return s[startIndex..];
	}

	/// <summary>
	/// Substring with length that doesn't throw ArgumentOutOfRangeException and silently return empty string. Could be simplified with Range indexing in C# 10 if we upgrade one day
	/// </summary>
	/// <param name="s">The string s.</param>
	/// <param name="startIndex">The start index.</param>
	/// <param name="length">The length.</param>
	/// <returns></returns>
	public static string SubstringSafe(this string s, int startIndex, int length) {
		if (string.IsNullOrEmpty(s)) {
			return s;
		}
		if (startIndex > s.Length) {
			return string.Empty;    //don't throw ArgumentOutOfRangeException
		}
		if (startIndex < 0) {
			int adjustedStart = Math.Max(0, s.Length + startIndex);   //Index from the right. Adding startIndex will actually subtract since it's negative
			return s.Substring(adjustedStart, Math.Min(length, s.Length - adjustedStart));
		}
		return s.Substring(startIndex, Math.Min(length, s.Length - startIndex));
	}
}
