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
    public static class Program
    {
        /// <summary>Path to the user's desktop folder</summary>
        public static readonly string DesktopPath = Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory );

        public static readonly string BasePath = Path.Combine( DesktopPath, "Facesheets" );

        /// <summary>Main entry class</summary>
        /// <param name="args">any future command line args</param>
        /// <returns>exit value</returns>
        public static int Main( string[] args )
        {
            Console.WriteLine( "Starting..." );

            // get the environment var
            string env = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" ) ?? "Development";

            using PdfDocument document = PdfDocument.Open( Path.Combine( BasePath, "Facesheets-5.pdf" ) );

            PatientInfo? curPatient = null;

            List<PatientInfo> patients = new List<PatientInfo>();

            int curPageNum = 1;

            while ( curPageNum <= document.NumberOfPages )
            {
                Page page = document.GetPage( curPageNum );

                var pageLines = ExtractPageLines( page );

                PatientInfo? nextPatient = IsStart( pageLines );

                // start a new patient
                if ( nextPatient != null )
                {
                    // if queue not empty - write out new PDF
                    //if ( currentPages.Count > 0 )
                    //WriteFacesheet( document, curPatient?.LastName, currentPages.ToArray() );

                    // remember the previous patient
                    if ( curPatient != null )
                        patients.Add( curPatient );

                    //currentPages.Clear();
                    curPatient = nextPatient;
                }
                // PDF doesn't start on a new patient
                else if ( curPageNum == 1 )
                {
                    Console.WriteLine( "PDF doesn't start on a new patient" );
                    break;
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

            //// 1 indexed
            //for ( int p = 1; p <= document.NumberOfPages; ++p )
            //{
            //    Page page = document.GetPage( p );

            //    var pageLines = ExtractPageLines( page );

            //    PatientInfo? nextPatient = IsStart( pageLines );

            //    // start a new patient
            //    if ( nextPatient != null )
            //    {
            //        // if queue not empty - write out new PDF
            //        if ( currentPages.Count > 0 )
            //            WriteFacesheet( document, curPatient?.LastName, currentPages.ToArray() );

            //        currentPages.Clear();
            //        curPatient = nextPatient;
            //    }

            //    // queue this page
            //    currentPages.Enqueue( p );
            //}

            //// write the last facesheet
            //if ( currentPages.Count > 0 )
            //    WriteFacesheet( document, curPatient?.LastName, currentPages.ToArray() );

            Console.WriteLine( "Stopping..." );

            return 0;
        }

        /// <summary></summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static List<string> ExtractPageLines( Page page )
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
        public static PatientInfo? IsStart( List<string> lines )
        {
            Regex regex = new Regex( @"^Patient Name:([-,\w ]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

            foreach ( string line in lines )
            {
                Match match = regex.Match( line.Trim() );

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
        public static void WriteFacesheet( PdfDocument pdf, string filename, params int[] pages )
        {
            if ( pdf == null || pages.Length < 1 )
                return;

            // need a better default
            if ( String.IsNullOrWhiteSpace( filename ) )
                filename = $"facesheet-{DateTimeOffset.UtcNow:yyyy-MM-dd}";

            filename = filename.Trim().Replace(',', '-');

            var builder = new PdfDocumentBuilder();
            Array.ForEach( pages, p => builder.AddPage( pdf, p ) );

            var fileBytes = builder.Build();
            var output = Path.Combine(BasePath, $"{filename}.pdf" );
            File.WriteAllBytes( output, fileBytes );
        }

    }
}



//var documentText = new StringBuilder();
//using var pdf = new PdfDocument( Path.Combine( DesktopPath, "Facesheets.pdf" ) );

//foreach (PdfPage page in pdf.Pages)
//{
//    string searchableText = page.GetText();
//    // if contains Patient - start of document
//    // keep reading until next Patient
//}

//using PdfDocument extracted = pdf.ExtractPages( 0, 3 );