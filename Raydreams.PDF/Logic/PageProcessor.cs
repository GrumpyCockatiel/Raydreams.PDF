using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace Raydreams.PDF
{
    /// <summary>Page Processor interface</summary>
    public interface IPageProcessor
    {
        Facility Location { get; }

        string? ExtractName();

        DateTime? ExtractDOB();

        Sex ExtractSex();
    }

    /// <summary>Base Parser</summary>
    public abstract class BasePageProcessor
    {
        protected List<string> lines = new List<string>();

        protected List<TextBlock> blocks;

        public BasePageProcessor( IEnumerable<TextBlock> txt )
        {
            blocks = txt.ToList();

            // Extract ALL text now
            foreach ( var block in txt )
            {
                lines.Add( block.Text.Normalize( NormalizationForm.FormKC ) );
            }
        }
    }

    /// <summary>Cornerstone Parser</summary>
    public class CornerstonePageProcessor : BasePageProcessor, IPageProcessor
    {
        public CornerstonePageProcessor( IEnumerable<TextBlock> txt ) : base( txt )
        {
        }

        /// <summary></summary>
        public Facility Location { get => Facility.Cornerstone; }

        /// <summary></summary>
        public DateTime? ExtractDOB()
        {
            Regex dobPat = new Regex( @"xxx-xx-9999 (\d\d/\d\d/\d\d) ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );

            foreach ( string line in lines )
            {
                Match match = dobPat.Match( line.Trim() );

                if ( match.Success && match.Groups.Count > 1 )
                {
                    return ( DateTime.TryParse( match.Groups[1].Value, out DateTime dob ) ) ? dob : null;
                }
            }

            return null;
        }

        /// <summary></summary>
        public string? ExtractName()
        {
            // find the label text block
            var pBlock = blocks.Where( b => b.Text.Trim().Equals( "patient", StringComparison.InvariantCultureIgnoreCase ) );

            // find the closet text block that is just below
            TextBlock? nameBlock = pBlock.First().JustUnder( this.blocks );

            return nameBlock?.Text;
        }

        /// <summary></summary>
        /// <remarks>Cant find a good parse for Sex yet</remarks>
        public Sex ExtractSex()
        {
            return Sex.Undetermined;
        }
    }

    /// <summary>Memorial Hermann Facesheet Parser</summary>
    public class MHPageProcessor : BasePageProcessor, IPageProcessor
    {
        public MHPageProcessor( IEnumerable<TextBlock> txt ) : base( txt )
        {
        }

        /// <summary></summary>
        public Facility Location { get => Facility.MemorialHermann; }

        /// <summary></summary>
        public DateTime? ExtractDOB()
        {
            Regex dobPat = new Regex( @"^dob: (\d\d/\d\d/\d{4})$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant );

            foreach ( string line in lines )
            {
                Match match = dobPat.Match( line.Trim() );

                if ( match.Success && match.Groups.Count > 1 )
                {
                    return ( DateTime.TryParse( match.Groups[1].Value, out DateTime dob ) ) ? dob : null;
                }
            }

            return null;
        }

        /// <summary>v a t i e n t Name: BUBBA, MACK</summary>
        public string? ExtractName()
        {
            Regex namePat = new Regex( @"^Patient Name:(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant );

            foreach ( string line in lines )
            {
                Match nameMatch = namePat.Match( line.Trim() );

                if ( nameMatch.Success && nameMatch.Groups.Count > 1 )
                    return nameMatch.Groups[1].Value.Clean();
            }

            return null;
        }

        /// <summary></summary>
        /// <returns></returns>
        public Sex ExtractSex()
        {
            Regex pattern = new Regex( @"^Sex:([\w ]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant );

            foreach ( string line in lines )
            {
                Match match = pattern.Match( line.Trim() );

                if ( match.Success && match.Groups.Count > 1 )
                    return match.Groups[1].Value.ParseSex();
            }

            return Sex.Undetermined;
        }
    }

    /// <summary>Memorial Hermann Facesheet Parser</summary>
    public class MethodistPageProcessor : BasePageProcessor, IPageProcessor
    {
        public MethodistPageProcessor(IEnumerable<TextBlock> txt) : base(txt)
        {
        }

        /// <summary></summary>
        public Facility Location { get => Facility.MemorialHermann; }

        /// <summary></summary>
        public DateTime? ExtractDOB()
        {
            Regex dobPat = new Regex(@"^dob: (\d\d/\d\d/\d{4})$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

            foreach (string line in lines)
            {
                Match match = dobPat.Match(line.Trim());

                if (match.Success && match.Groups.Count > 1)
                {
                    return (DateTime.TryParse(match.Groups[1].Value, out DateTime dob)) ? dob : null;
                }
            }

            return null;
        }

        /// <summary></summary>
        public string? ExtractName()
        {
            // Maya, Delfina (MRN: 110302983) DOB: 1212411944

            Regex namePat = new Regex( @"^\s*(\w[a-zA-Z,\- ]+)\s+\(MRN:", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

            foreach (string line in lines)
            {
                Match nameMatch = namePat.Match(line.Trim());

                if (nameMatch.Success && nameMatch.Groups.Count > 1)
                    return nameMatch.Groups[1].Value.Clean();
            }

            return null;
        }

        /// <summary></summary>
        /// <returns></returns>
        public Sex ExtractSex()
        {
            Regex pattern = new Regex(@"^Sex:([\w ]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

            foreach (string line in lines)
            {
                Match match = pattern.Match(line.Trim());

                if (match.Success && match.Groups.Count > 1)
                    return match.Groups[1].Value.ParseSex();
            }

            return Sex.Undetermined;
        }
    }

    /// <summary>Null Parser</summary>
    public class NullPageProcessor : IPageProcessor
    {
        public Facility Location { get => Facility.Unknown; }

        public DateTime? ExtractDOB() => null;

        public string? ExtractName() => null;

        public Sex ExtractSex() => Sex.Undetermined;
    }
}

