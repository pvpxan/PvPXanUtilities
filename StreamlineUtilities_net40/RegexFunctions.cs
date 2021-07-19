using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamlineUtilities
{
    // START RegexMatch Class -----------------------------------------------------------------------------------------------------------
    public static class RegexMatch
    {
        // Tests for White spaces or empty strings. If white space exists, it will return true.
        public static bool WhiteSpaceMatch(string input)
        {
            return regexPatternMatch(input, @"[\s]");
        }

        // Checks if a string has special characters.
        public static bool SpecialCharactersMatch(string input)
        {
            return regexPatternMatch(input, @"[_]|[^\w\s]");
        }

        // Checks if a string has special characters and no whitespace.
        public static bool SpecialCharactersNoWhiteSpaceMatch(string input)
        {
            return regexPatternMatch(input, @"[_]|[^\w]");
        }

        // Checks if the string is an integer value.
        public static bool NumericMatch(string input)
        {
            return regexPatternMatch(input, @"^[\d]+$");
        }

        // Checks if the contains legal file path characters.
        public static bool PathMatch(string input)
        {
            return regexPatternMatch(input, @"^[\w\s\.\-\:\\]+");
        }

        // Checks if the string is with certain special characters.
        public static bool CommonNameMatch(string input)
        {
            return regexPatternMatch(input, @"^[\w\s\.\-\&\@]+$");
        }

        // Performs the regex operation.
        private static bool regexPatternMatch(string input, string pattern)
        {
            if (input == null || pattern == null)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(input, pattern);
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error processing regex match check of string. Input: " + input + " |  Pattern: " + pattern, Ex);
                return false;
            }
        }
    }
    // END RegexMatch Class -------------------------------------------------------------------------------------------------------------

    // START RegexReplace Class ---------------------------------------------------------------------------------------------------------
    public static class RegexReplace
    {
        // Clears White spaces.
        public static string WhiteSpaceRemove(string input)
        {
            return regexPatternReplace(input, @"[\s]", "");
        }

        // Replaces white spaces with a specific string.
        public static string WhiteSpaceReplace(string input, string replacement)
        {
            return regexPatternReplace(input, @"[\s]", replacement);
        }

        public static string SpecialCharactersRemove(string input)
        {
            return regexPatternReplace(input, @"[_]|[^\w\s]", "");
        }

        public static string SpecialCharactersWithSpacesRemove(string input)
        {
            return regexPatternReplace(input, @"[_]|[^\w]", "");
        }

        public static string MakeNumeric(string input)
        {
            return regexPatternReplace(input, @"[^\d]", "");
        }

        public static string RemoveExtraSpaces(string input)
        {
            return regexPatternReplace(input, @"\  +", " ");
        }

        public static string MakePath(string input)
        {
            return regexPatternReplace(input, @"[^\w\s\-\.\:\\]+", "");
        }

        public static string MakeCommonName(string input)
        {
            return regexPatternReplace(input, @"[^\w\s\.\-\&\@]", "");
        }

        private static string regexPatternReplace(string input, string pattern, string replacement)
        {
            if (input == null || replacement == null)
            {
                return "";
            }

            if (pattern == null)
            {
                return input;
            }

            try
            {
                return Regex.Replace(input, pattern, replacement);
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("Error processing regex replacement. Input: " + input + " |  Pattern: " + pattern + " | Replacement: " + replacement, Ex);
                return "";
            }
        }
    }
    // END RegexReplace Class -----------------------------------------------------------------------------------------------------------
}
