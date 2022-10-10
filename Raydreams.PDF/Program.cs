using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Writer;

namespace Raydreams.PDF
{
    public class Program
    {
        /// <summary>Path to the user's desktop folder</summary>
        public static readonly string DesktopPath = Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory );

        public static readonly string BasePath = Path.Combine( DesktopPath, "Facesheets" );

        /// <summary>Main entry class</summary>
        /// <param name="args">any future command line args</param>
        /// <returns>exit value</returns>
        public static int Main( string[] args )
        {
            Program app = new Program();
            app.Run( "Facesheets-5.pdf" );

            return 0;
        }

        public void Run(string facesheet)
        {
            Console.WriteLine( "Starting..." );

            // get the environment var
            string env = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" ) ?? "Development";

            using PdfDocument document = PdfDocument.Open( Path.Combine( BasePath, facesheet ) );

            List<PatientInfo> patients = new List<PatientInfo>();

            // 1 indexed
            int curPageNum = 1;

            // page 1 should always be a new Patient Header
            Page page = document.GetPage( curPageNum++ );

            // get all the lines on this page
            var pageLines = ExtractPageLines( page );

            // does this page start a new patient
            PatientInfo? curPatient = Parse( pageLines );

            if ( curPatient == null )
            {
                Console.WriteLine( "PDF doesn't start on a new patient" );
                return;
            }

            while ( curPageNum <= document.NumberOfPages )
            {
                // get the current page
                page = document.GetPage( curPageNum );

                // get all the lines on this page
                pageLines = ExtractPageLines( page );

                // does this page start a new patient
                PatientInfo? nextPatient = Parse( pageLines );

                // starting a new patient
                if ( nextPatient != null )
                {
                    // remember the previous patient
                    if ( curPatient != null )
                        patients.Add( curPatient );

                    curPatient = nextPatient;
                }

                // queue this page
                if ( curPatient != null )
                    curPatient.Pages.Enqueue( curPageNum );

                ++curPageNum;
            }

            // write out all the facesheets
            foreach ( PatientInfo p in patients )
            {
                WriteFacesheet( document, p.LastName, p.Pages.ToArray() );
            }

            Console.WriteLine( "Stopping..." );
        }

        /// <summary></summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public List<string> ExtractPageLines( Page page )
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

            var words = wordExtractor.GetWords( letters );

            // 2. Segment page
            var pageSegmenter = DocstrumBoundingBoxes.Instance;
            var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { };

            var textBlocks = pageSegmenter.GetBlocks( words );

            // 3. Postprocessing
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            var orderedTextBlocks = readingOrder.Get( textBlocks );

            // 4. Extract text
            foreach ( var block in orderedTextBlocks )
            {
                sb.Append( block.Text.Normalize( NormalizationForm.FormKC ) ); // normalise text
                sb.AppendLine();

                lines.Add( block.Text.Normalize( NormalizationForm.FormKC ) );
            }

            //return sb.ToString();
            return lines;
        }

        /// <summary></summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public PatientInfo? Parse( List<string> lines )
        {
            Regex namePat = new Regex( @"^Patient Name:([-,\w ]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

            foreach ( string line in lines )
            {
                Match match = namePat.Match( line.Trim() );

                if ( match.Success && match.Groups.Count > 1 )
                {
                    return new PatientInfo { Name = match.Groups[1].Value };
                }
            }

            return null;
        }

        /// <summary></summary>
        /// <param name="pdf"></param>
        /// <param name="filename"></param>
        /// <param name="pages"></param>
        public void WriteFacesheet( PdfDocument pdf, string filename, params int[] pages )
        {
            if ( pdf == null || pages.Length < 1 )
                return;

            // need a better default
            if ( String.IsNullOrWhiteSpace( filename ) )
                filename = $"facesheet-{String.Join( String.Empty, pages )}";
            else
                filename = filename.Trim().Replace(',', '-');

            var builder = new PdfDocumentBuilder();
            Array.ForEach( pages, p => builder.AddPage( pdf, p ) );

            var fileBytes = builder.Build();
            var output = Path.Combine(BasePath, $"{filename}.pdf" );
            File.WriteAllBytes( output, fileBytes );

            Console.WriteLine( $"Wrote Facesheet for {filename}" );
        }

    }
}
