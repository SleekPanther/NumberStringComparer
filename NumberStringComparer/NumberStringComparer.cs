using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NumberStringComparer;
/// <summary>
/// Used to compare potentially numeric and non-numeric sequences.
/// This puts numbers and comma-separated numbers before letters.
/// If an item cannot be parsed completely as a number, it is parsed into a list of its numeric and string parts which are then compared.
/// </summary>
/// <typeparam name="T">The type of collection being compared</typeparam>
/// <seealso cref="System.Collections.Generic.IComparer<T>" />
public sealed class NumberStringComparer<T> : IComparer<T>
{
	public static readonly HashSet<Type> allowedPropertyTypes = [typeof(string), typeof(int), typeof(double), typeof(float), typeof(decimal), typeof(long), typeof(short),];
	private readonly string? propertyName = null;	//optional, used for Object comparison
	private static readonly Func<T, object>? keyExtractor = InitializeKeyExtractor();
	private NumberStringComparer() { }
	private NumberStringComparer(string propertyName) {
		this.propertyName = propertyName;
	}
	private static readonly ConcurrentDictionary<string, NumberStringComparer<T>> comparers = new();

	public static void ThrowUnsupportedTypeException(string propertyName, Type type) => throw new InvalidOperationException($"{propertyName} is {(type == null ? " null " : type.Name)}");
	public static NumberStringComparer<T> GetComparer() {
		Type type = typeof(T);
		if (IsComplexType(type)) throw new InvalidOperationException($"{type.Name} is a complex type, {nameof(GetComparer)} should only be called with primitive types of {typeof(KeyValuePair<,>).Name}");
		return comparers.GetOrAdd(type.FullName!, _ => new NumberStringComparer<T>());
	}
	public static NumberStringComparer<T> GetObjectComparer(string propertyName) {
		if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
		Type type = typeof(T);
		if (!IsComplexType(type)) throw new InvalidOperationException($"{type.Name} is not a complex type, {nameof(GetObjectComparer)} should only be called with complex types");
		comparers.TryGetValue(propertyName, out var comparer);
		if(comparer == null) {
			var property = type.GetProperty(propertyName);
			if (property == null) throw new InvalidOperationException($"{type.Name} does not have property {propertyName}");
			if (!IsValidType(property.PropertyType)) ThrowUnsupportedTypeException(propertyName, property.GetType());
			comparer = new NumberStringComparer<T>(propertyName);
		}
		return comparer;
	}
	public static bool IsComplexType(Type type) => !type.IsPrimitive && type != typeof(string) && type != typeof(decimal) && !IsKeyValuePair(type);
	public static bool IsValidType(Type type) {
		if (allowedPropertyTypes.Contains(type)) {
			return true;
		}
		if (IsKeyValuePair(type)) {
			Type genericType1 = type.GetGenericArguments()[0];
			return allowedPropertyTypes.Contains(genericType1);	//for KVP we use the type of the Key
		}
		return false;
	}
	public static bool IsKeyValuePair(Type type) {
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
	}

	private static Func<T, object>? InitializeKeyExtractor() {
		Type type = typeof(T);
		if (!IsKeyValuePair(type)) return null;
		
		var keyProperty = type.GetProperty("Key")!;
		var parameter = Expression.Parameter(type, "kvp");
		var keyAccess = Expression.Property(parameter, keyProperty);
		var boxed = Expression.Convert(keyAccess, typeof(object));
		return Expression.Lambda<Func<T, object>>(boxed, parameter).Compile();
	}

	public int Compare(T? x, T? y) {
		Type type = typeof(T);
		if(IsKeyValuePair(type)) {
			return NumberString<T>.Parse(keyExtractor!(x!)).CompareTo(NumberString<T>.Parse(keyExtractor!(y!)));
		}
		if (IsComplexType(type)) {
			if (string.IsNullOrWhiteSpace(propertyName)) throw new InvalidOperationException($"{propertyName} is empty and must be specified for object comparison on complex types. Type = {type}");
			return NumberString<T>.Parse(x!, propertyName).CompareTo(NumberString<T>.Parse(y!, propertyName));
		}
		return NumberString<T>.Parse(x?.ToString()).CompareTo(NumberString<T>.Parse(y?.ToString()));
	}

	public readonly struct NumberString<U> : IComparable<NumberString<U>> 
	{
		public readonly double? number;
		public readonly string text;
		private readonly IReadOnlyList<string>? parts;
		public NumberString(double? number, string text) {
			this.number = number;
			this.text = text;
			this.parts = null;
		}
		public NumberString(double? number, string text, IReadOnlyList<string>? parts) {
			this.number = number;
			this.text = text;
			this.parts = parts;
		}

		public static NumberString<U> Parse(U anObject, string propertyName) {
			if(anObject == null) throw new ArgumentNullException(nameof(anObject));
			var property = anObject.GetType().GetProperty(propertyName);
			object? value; 
			if (property!.GetAccessors(true).Any(x => x.IsStatic)) {
				value = property.GetValue(null);
			}
			else {
				value = property.GetValue(anObject);
			}
			return Parse(value);
		}
		public static NumberString<U> Parse(object? value) {
			if(value == null) {
				return new NumberString<U>(null, string.Empty);
			}
			Type type = value.GetType();
			if (IsComplexType(type)) {
				throw new InvalidOperationException($"Cannot call this version of {nameof(Parse)} with complex object (Type = {type.Name}). Use the other version passing and object instance and propertyName");
			}

			string textValue = value.ToString()!;
			if (textValue.Contains(',')) {	//double.TryParse thinks 1,2 is 12 so we must handle commas ourselves
				return new NumberString<U>(null, textValue, GetParts(textValue));
			}
			if (double.TryParse(textValue, out double num)) {
				return new NumberString<U>(num, textValue);
			}
			return new NumberString<U>(null, textValue, GetParts(textValue));
		}
		public readonly int CompareTo(NumberString<U> other) {
			if (number == null || other.number == null) {
				return CompareToPartial(this, other);
			}
			return ((double)number).CompareTo((double)other.number);	//can't be null at this point so casting won't fail
		}

		private static int CompareToPartial(NumberString<U> a, NumberString<U> b) {
			var aParts = a.parts ?? GetParts(a.text);
			var bParts = b.parts ?? GetParts(b.text);

			if (Enumerable.SequenceEqual(aParts!, bParts!)) {
				return 0;
			}

			int result = 0;
			int i = 0;
			//compare each part until we find a difference or reach the end of the shorter list
			for (; i < aParts!.Count && i < bParts!.Count && result == 0; i++) {
				bool aIsNum = double.TryParse(aParts[i], out double num1);
				bool bIsNum = double.TryParse(bParts[i], out double num2);
				
				if (aIsNum && bIsNum) {	//number vs number
					result = num1.CompareTo(num2);
				}
				else {	//number vs string OR string vs number OR string vs string - all use string comparison
					result = aParts[i].CompareTo(bParts[i]);
				}
			}

			if(result == 0) {
				if (i < aParts!.Count) return 1;  // a has more parts
				if (i < bParts!.Count) return -1; // b has more parts
			}
			return result;
		}

		public static IReadOnlyList<string>? GetParts(string text) {
			if (text == null) return null;
			if (text.Length == 0) return Array.Empty<string>();
			if (text.Length == 1) return new[] { text[0].ToString() };
			
			ReadOnlySpan<char> span = text.AsSpan();
			
			// Comma-separated: parse manually using Span to avoid Split() allocations. Also avoids problems where double.TryParse ignores commas
			if (text.Contains(',')) {
				var parts = new List<string>();
				int start = 0;
				for (int i = 0; i < span.Length; i++) {
					if (span[i] == ',') {
						// Slice span from start to comma, trim whitespace, convert to string only if non-empty
						var part = span.Slice(start, i - start).Trim();
						if (part.Length > 0) {
							parts.Add(part.ToString());
						}
						start = i + 1;
					}
				}
				// Handle remaining text after last comma
				var lastPart = span.Slice(start).Trim();
				if (lastPart.Length > 0) {
					parts.Add(lastPart.ToString());
				}
				return parts;
			}
			
			// If entire text is a valid number, return as single part
			if (double.TryParse(span, out double _)) {
				return new[] { text };
			}
			
			// Check if text contains any digits (simple loop instead of LINQ to avoid lambda allocation)
			bool hasDigit = false;
			for (int i = 0; i < span.Length; i++) {
				if (char.IsDigit(span[i])) {
					hasDigit = true;
					break;
				}
			}
			// If no digits, return entire text as single part
			if (!hasDigit) {
				return new[] { text };
			}

			// Mixed alphanumeric: split into digit and non-digit parts using state machine
			var result = new List<string>();
			int partStart = 0;
			bool wasDigit = char.IsDigit(span[0]);
			
			// Simple state machine: track when digit/non-digit state changes
			for (int i = 1; i < span.Length; i++) {
				bool isDigit = char.IsDigit(span[i]);
				if (isDigit != wasDigit) {  // State changed from digit to non-digit or vice versa
					result.Add(text.Substring(partStart, i - partStart));
					partStart = i;
					wasDigit = isDigit;
				}
			}
			result.Add(text.Substring(partStart));  // Add final part
			return result;
		}

		public override readonly string ToString() {
			return (number?.ToString() ?? "null")
				+ (text == null ? " (null)" : " (" + text + ")");
		}
	}
}
