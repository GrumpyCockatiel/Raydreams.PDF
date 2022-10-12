using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Raydreams.PDF
{
    /// <summary>Page Processor interface</summary>
    public interface IPageProcessor
    {
        Facility Location { get; }

        string? ExtractName();

        DateTime? ExtractDOB();
    }

    /// <summary></summary>
    public static class PageProcessorFactory
    {
        public static IPageProcessor MakeProcessor( Page page, Facility location )
        {
            IEnumerable<TextBlock> txt = ExtractPageBlocks( page );

            if ( location == Facility.Cornerstone )
                return new CornerstonePageProcessor( txt );
            else if ( location == Facility.MemorialHermann )
                return new MHPageProcessor( txt );

            return new NullPageProcessor();
        }

        /// <summary></summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static Facility Detect( Page page )
        {
            IEnumerable<TextBlock> txt = ExtractPageBlocks( page );

            foreach ( TextBlock block in txt )
            {
                string line = block.Text.Normalize( NormalizationForm.FormKC );

                if ( line.Contains( "cornerstone", StringComparison.InvariantCultureIgnoreCase ) )
                    return Facility.Cornerstone;
                else if ( line.Contains( "memorial", StringComparison.InvariantCultureIgnoreCase ) )
                    return Facility.MemorialHermann;
            }

            return Facility.Unknown;
        }

        /// <summary></summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IEnumerable<TextBlock> ExtractPageBlocks( Page page )
        {
            List<string> lines = new List<string>();
            var sb = new StringBuilder();

            // 0. Preprocessing
            var letters = page.Letters; // no preprocessing

            // 1. Extract words
            var wordExtractor = NearestNeighbourWordExtractor.Instance;

            var wordExtractorOptions = new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
            {
                Filter = ( pivot, candidate ) =>
                {
                    // check if white space (default implementation of 'Filter')
                    if ( string.IsNullOrWhiteSpace( candidate.Value ) )
                    {
                        // pivot and candidate letters cannot belong to the same word 
                        // if candidate letter is null or white space.
                        // ('FilterPivot' already checks if the pivot is null or white space by default)
                        return false;
                    }

                    // check for height difference
                    var maxHeight = Math.Max( pivot.PointSize, candidate.PointSize );
                    var minHeight = Math.Min( pivot.PointSize, candidate.PointSize );

                    if ( minHeight != 0 && maxHeight / minHeight > 2.0 )
                    {
                        // pivot and candidate letters cannot belong to the same word 
                        // if one letter is more than twice the size of the other.
                        return false;
                    }

                    // check for colour difference
                    var pivotRgb = pivot.Color.ToRGBValues();

                    var candidateRgb = candidate.Color.ToRGBValues();

                    if ( !pivotRgb.Equals( candidateRgb ) )
                    {
                        // pivot and candidate letters cannot belong to the same word 
                        // if they don't have the same colour.
                        return false;
                    }

                    return true;
                }
            };

            // get all the words using the filters
            var words = wordExtractor.GetWords( letters );

            // 2. Segment page
            var pageSegmenter = DocstrumBoundingBoxes.Instance;
            var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { };

            IReadOnlyList<TextBlock> textBlocks = pageSegmenter.GetBlocks( words );

            // 3. Postprocessing
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            IEnumerable<TextBlock> orderedTextBlocks = readingOrder.Get( textBlocks );

            return orderedTextBlocks;
        }
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

            // find the closet text block that comes after
            TextBlock? nameBlock = pBlock.First().JustUnder( this.blocks );

            return nameBlock?.Text;
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

        /// <summary></summary>
        public string? ExtractName()
        {
            Regex namePat = new Regex( @"^Patient Name:([-,\w ]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant );

            foreach ( string line in lines )
            {
                Match nameMatch = namePat.Match( line.Trim() );

                if ( nameMatch.Success && nameMatch.Groups.Count > 1 )
                    return nameMatch.Groups[1].Value;
            }

            return null;
        }
    }

    /// <summary>Null Parser</summary>
    public class NullPageProcessor : IPageProcessor
    {
        public Facility Location { get => Facility.Unknown; }

        public DateTime? ExtractDOB() => null;

        public string? ExtractName() => null;
    }
}

