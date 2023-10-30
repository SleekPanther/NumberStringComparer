using System.Collections.Concurrent;

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
	public static readonly HashSet<Type> allowedPropertyTypes = new () { typeof(string), typeof(int), typeof(double), typeof(float), typeof(decimal), typeof(long), typeof(short), };
	private readonly string? propertyName = null;	//optional, used for Object comparison
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

	public int Compare(T? x, T? y) {
		Type type = typeof(T);
		if(IsKeyValuePair(type)) {
			return NumberString<T>.Parse(((dynamic)x!).Key).CompareTo(NumberString<T>.Parse(((dynamic)y!).Key));
		}
		if (IsComplexType(type)) {
			if (string.IsNullOrWhiteSpace(propertyName)) throw new InvalidOperationException($"{propertyName} is empty and must be specified for object comparison on complex types. Type = {type}");
			return NumberString<T>.Parse(x!, propertyName).CompareTo(NumberString<T>.Parse(y!, propertyName));
		}
		return NumberString<T>.Parse(x?.ToString()).CompareTo(NumberString<T>.Parse(y?.ToString()));
	}

	public struct NumberString<U> : IComparable<NumberString<U>> 
	{
		public readonly double? number;
		public readonly string text;
		public IReadOnlyList<string>? parts;
		public NumberString(double? number, string text) {
			this.number = number;
			this.text = text;
			this.parts = null;
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
			if (IsKeyValuePair(type)) {			//normally CompareTo just sends the key from KVP but just in case, we handle it just in case
				value = ((dynamic)value).Key;
			}

			if (value.ToString()!.Contains(',')) {	//double.TryParse thinks 1,2 is 12 so we must handle commas ourselves
				return new NumberString<U>(null, value.ToString()!);
			}
			if (double.TryParse(value.ToString(), out double num)) {
				return new NumberString<U>(num, value.ToString()!);
			}
			return new NumberString<U>(null, value.ToString()!);
		}
		public readonly int CompareTo(NumberString<U> other) {
			if (number == null || other.number == null) {
				return CompareToPartial(this, other);
			}
			return ((double)number).CompareTo((double)other.number);	//can't be null at this point so casting won't fail
		}

		private static int CompareToPartial(NumberString<U> a, NumberString<U> b) {
			a.parts = GetParts(a.text);
			b.parts = GetParts(b.text);

			if (Enumerable.SequenceEqual(a.parts!, b.parts!)) {
				return 0;
			}

			int result = 0;
			int i = 0;
			for (; i < a.parts!.Count && i < b.parts!.Count && result == 0; i++) {
				if (double.TryParse(a.parts[i], out double num1)) {
					if (double.TryParse(b.parts[i], out double num2)) {	//number vs number
						result = num1.CompareTo(num2);
					}
					else {	//number vs string
						result = a.parts[i].CompareTo(b.parts[i]);
					}
				}
				else {
					if (double.TryParse(b.parts[i], out double num2)) {		//string vs number
						result = a.parts[i].CompareTo(b.parts[i]);
					}
					else {	//string vs string
						result = a.parts[i].CompareTo(b.parts[i]);
					}
				}
			}

			if(result == 0 && i < b.parts!.Count) {	//b still has more, return -1 to indicate it should appear after the first (shorter) item
				result = -1;
			}
			return result;
		}

		public static IReadOnlyList<string>? GetParts(string text) {
			if (text == null) return null;
			if (text.Length == 0) return Array.Empty<string>().AsReadOnly();
			if (text.Length == 1) return new List<string>() { text[0].ToString() }.AsReadOnly();
			if (text.Contains(',')) {   //must check this before double.TryParse because that ignores commas
				return text
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(s => s.Trim())
					.Where(s => !string.IsNullOrEmpty(s))
					.ToList()
					.AsReadOnly();
			}
			if (double.TryParse(text, out double _ ) 
				|| text.All(ch => !char.IsDigit(ch))) return new List<string>() { text };

			int left = 0;
			bool wasNumber = false;
			char extra = default;
			var parts = new List<string>();
			bool breakOuter = false;
			for (int i = 0; i < text.Length; i++) {
				char ch = text[i];
				for(; double.TryParse(ch + "", out var digit); i++) {
					if (i > 0 && (!wasNumber || i == text.Length - 1)) {
						if (i == text.Length - 1) {
							if (!wasNumber) {
								parts.Add(text[left..i]);
								extra = text[i];
							}
							else {
								parts.Add(text[left..(i+1)]);
							}
							breakOuter = true;
							break;
						}
						else {
							parts.Add(text[left..i]);
						}
						left = i;
					}
					wasNumber = true;
					if(i < text.Length -1) {
						ch = text[i+1];
					}
				}
				if (breakOuter) {
					break;
				}

				for (; !double.TryParse(ch + "", out var digit); i++) {
					if (i > 0 && (wasNumber || i == text.Length - 1)) {
						if (i == text.Length - 1) {
							if (wasNumber) {
								parts.Add(text[left..(i +0-1)]);
								extra = text[i];
							}
							else {
								parts.Add(text[left..(i +1)]);
							}
							breakOuter = true;
							break;
						}
						else {
							parts.Add(text[left..i]);
						}
						left = i;
					}
					wasNumber = false;
					if(i < text.Length -1) {
						ch = text[i+1];
					}
				}
				if (breakOuter) {
					break;
				}
				if (i > 0 && i < text.Length) {
					i--;	//go back 1 character so the other loop can start from where the last one failed to parse
				}
			}

			if (extra != default(char)) {
				parts.Add(extra.ToString());
			}
			return parts;
		}

		public override readonly string ToString() {
			return (number?.ToString() ?? "null")
				+ (text == null ? " (null)" : " (" + text + ")");
		}
	}
}
