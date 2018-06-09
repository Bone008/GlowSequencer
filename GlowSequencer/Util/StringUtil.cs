
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

        /// <summary>Returns the grammatically correct version of a count of things.</summary>
        /// <param name="number">number of things, will be included in the output</param>
        /// <param name="singularThing">singular word of the thing being counted</param>
        /// <param name="pluralThing">optional plural version of the thing being counted; defaults to the singular version + "s"</param>
        /// <returns></returns>
        public static string Pluralize(int number, string singularThing, string pluralThing = null)
        {
            if (number == 1) return number + " " + singularThing;
            else return number + " " + (pluralThing ?? (singularThing + "s"));
        }
    }
}
