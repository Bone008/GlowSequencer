
namespace GlowSequencer.Util
{
    public static class StringUtil
    {
        /// <summary>Converts null input to the empty string, otherwise returns the input unmodified.</summary>
        public static string NullToEmpty(string input) => (input ?? "");

        /// <summary>Converts the empty string to null, otherwise returns the input unmodified.</summary>
        public static string EmptyToNull(string input) => (input == "" ? null : input);

        /// <summary>Converts all whitespace-only inputs to null, otherwise returns the input unmodified.</summary>
        public static string WhiteSpaceToNull(string input) => (string.IsNullOrWhiteSpace(input) ? null : input);
    }
}
