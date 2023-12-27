using System;

namespace FortRise;


/// <summary>
/// Collections of utilities for strings
/// </summary>
public static class StringUtils
{
    ///<summary>
    /// Add spaces to the text from capital letters. (ex. LastManStanding -> Last Man Standing).
    ///</summary>
    /// <param name="characters">An input text to transform with</param>
    /// <returns>A text with spaced between capital letters</returns>
    public static string SeparateCases(string characters) 
    {
        return SeparateCases(characters.AsSpan());
    }

    ///<summary>
    /// Add spaces to the text from capital letters. (ex. LastManStanding -> Last Man Standing).
    ///</summary>
    /// <param name="characters">A span containing the pointer to string</param>
    /// <returns>A text with spaced between capital letters</returns>
    public static string SeparateCases(ReadOnlySpan<char> characters) 
    {
        int upper = 0;
        for (int i = 1; i < characters.Length; i++) 
        {
            if (char.IsUpper(characters[i])) 
                upper++;
        }

        Span<char> dest = stackalloc char[characters.Length + upper];

        int offset = 0;
        for (int i = 0; i < characters.Length; i++) 
        {
            if (i == 0) 
            {
                dest[i] = characters[i];
                continue;
            }

            if (i < characters.Length - 1 && char.IsUpper(characters[i + 1])) 
            {
                dest[i + offset] = characters[i];
                dest[i + offset + 1] = ' ';
                offset++;
                continue;
            }
            dest[i + offset] = characters[i];
        }
        return dest.ToString();
    } 

    /// <summary>
    /// Transform the text into a title case (ex. sacred ground -> Sacred Ground) 
    /// </summary>
    /// <param name="characters">An input text to transform with</param>
    /// <returns>A title cased text</returns>
    public static string ToTitleCase(string characters)  
    {
        return ToTitleCase(characters.AsSpan());
    }

    /// <summary>
    /// Transform the text into a title case (ex. sacred ground -> Sacred Ground) 
    /// </summary>
    /// <param name="characters">A span containing the pointer to string</param>
    /// <returns>A title cased text</returns>
    public static string ToTitleCase(ReadOnlySpan<char> characters) 
    {
        Span<char> s = stackalloc char[characters.Length];
        characters.ToLowerInvariant(s);
        bool capital = true;
        for (int i = 0; i < s.Length; i++)
        {
            char current = s[i];
            if (char.IsWhiteSpace(current))
            {
                capital = true;
                continue;
            }
            if (!capital)
                continue;
            
            s[i] = char.ToUpper(current);
            capital = false;
        }
        return s.ToString();
    }

    // Based on:
    // https://www.meziantou.net/split-a-string-into-lines-without-allocation.htm
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str, char separator)
    {
        // LineSplitEnumerator is a struct so there is no allocation here
        return new LineSplitEnumerator(str, separator);
    }

    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;
        private char separator;

        public LineSplitEnumerator(ReadOnlySpan<char> str, char separator)
        {
            _str = str;
            Current = default;
            this.separator = separator;
        }

        // Needed to be compatible with the foreach operator
        public LineSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var span = _str;
            if (span.Length == 0) // Reach the end of the string
                return false;

            var index = span.IndexOf(separator);
            if (index == -1) // The string is composed of only one line
            {
                _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
            _str = span.Slice(index + 1);
            return true;
        }

        public LineSplitEntry Current { get; private set; }
    }

    public readonly ref struct LineSplitEntry
    {
        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
        {
            Line = line;
            Separator = separator;
        }

        public ReadOnlySpan<char> Line { get; }
        public ReadOnlySpan<char> Separator { get; }

        // This method allow to deconstruct the type, so you can write any of the following code
        // foreach (var entry in str.SplitLines()) { _ = entry.Line; }
        // foreach (var (line, endOfLine) in str.SplitLines()) { _ = line; }
        // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/deconstruct?WT.mc_id=DT-MVP-5003978#deconstructing-user-defined-types
        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Line;
            separator = Separator;
        }

        // This method allow to implicitly cast the type into a ReadOnlySpan<char>, so you can write the following code
        // foreach (ReadOnlySpan<char> entry in str.SplitLines())
        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    }
}