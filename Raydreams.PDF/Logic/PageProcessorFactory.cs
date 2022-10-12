using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Raydreams.PDF
{
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

        /// <summary>Extract PDF Page blocks</summary>
        /// <param name="page"></param>
        /// <returns></returns>
        /// <remarks>This holds the logic to decide what text is grouped together</remarks>
        public static IEnumerable<TextBlock> ExtractPageBlocks( Page page )
        {
            List<string> lines = new List<string>();
            var sb = new StringBuilder();

            // preprocessing
            var letters = page.Letters; // no preprocessing

            // extract words
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

            // segment page
            var pageSegmenter = DocstrumBoundingBoxes.Instance;
            var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { };

            IReadOnlyList<TextBlock> textBlocks = pageSegmenter.GetBlocks( words );

            // postprocessing
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            IEnumerable<TextBlock> orderedTextBlocks = readingOrder.Get( textBlocks );

            return orderedTextBlocks;
        }
    }
}

