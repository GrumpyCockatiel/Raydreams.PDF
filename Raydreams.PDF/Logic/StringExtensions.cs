using System;
using System.Text;

namespace Raydreams.PDF
{
    /// <summary>Utility String Extensions</summary>
    public static class StringExtensions
    {
        /// <summary></summary>
        /// <param name="ignoreCase">Ignore case by default</param>
        /// <returns></returns>
        /// <remarks>Case is ignored</remarks>
        public static Sex ParseSex( this string value )
        {
            if ( String.IsNullOrWhiteSpace( value ) )
                return default;

            value = value.Trim().ToLowerInvariant();

            if ( value[0] == 'm' )
                return Sex.Male;

            return ( value[0] == 'f' ) ? Sex.Female : Sex.Undetermined;
        }

        /// <summary>Converts a string to an enum value of enum T failing to default(T)</summary>
        /// <param name="ignoreCase">Ignore case by default</param>
        /// <returns></returns>
        /// <remarks>Case is ignored</remarks>
        public static T GetEnumValue<T>( this string value, bool ignoreCase = true ) where T : struct, IConvertible
        {
            T result = default( T );

            if ( String.IsNullOrWhiteSpace( value ) )
                return result;

            if ( Enum.TryParse<T>( value.Trim(), ignoreCase, out result ) )
                return result;

            return default( T );
        }

        /// <summary>Converts a string to an enum value with the specified default on fail</summary>
        /// <param name="def">Explicit default value if parsing fails</param>
        /// <param name="ignoreCase">Ignore case by default</param>
        /// <returns></returns>
        public static T GetEnumValue<T>( this string value, T def, bool ignoreCase = true ) where T : struct, IConvertible
        {
            T result = def;

            if ( String.IsNullOrWhiteSpace( value ) )
                return result;

            if ( Enum.TryParse<T>( value.Trim(), ignoreCase, out result ) )
                return result;

            return def;
        }

        /// <summary>Name cases a string so only the first letter of a word is upper and the rest are lower</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string NameCase( this string str )
        {
            StringBuilder sb = new StringBuilder();
            bool upper = true;

            str = str.TrimConsecutive( true );

            // iterate the string
            for ( int i = 0; i < str.Length; ++i )
            {
                if ( upper )
                    sb.Append( Char.ToUpper( str[i] ) );
                else
                    sb.Append( Char.ToLower( str[i] ) );

                // if this char is whitespace then the next char will be upper
                upper = Char.IsWhiteSpace( str, i );
            }

            return sb.ToString();
        }


        /// <summary>Removes consecutive white space internally within a string.</summary>
        /// <param name="trim">Whether to trim whitespace from the front and back of the string as well.</param>
        public static string TrimConsecutive( this string str, bool trim = true )
        {
            if ( trim )
                str = str.Trim();

            StringBuilder temp = new StringBuilder();

            for ( int i = 0; i < str.Length; ++i )
            {
                if ( i > 0 && Char.IsWhiteSpace( str[i] ) && Char.IsWhiteSpace( temp[temp.Length - 1] ) )
                    continue;

                temp.Append( str[i] );
            }

            return temp.ToString();
        }
    }
}

