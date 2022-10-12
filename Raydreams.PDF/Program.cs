using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
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

        /// <summary></summary>
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

        public void Run( string facesheet )
        {
            // get the environment var
            string env = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" ) ?? "Development";

            using PdfDocument document = PdfDocument.Open( Path.Combine( BasePath, facesheet ) );

            List<PatientInfo> patients = new List<PatientInfo>();

            // 1 indexed
            int curPageNum = 1;

            // hold a ref to the current patient
            PatientInfo? curPatient = null;

            do
            {
                // get the current page
                Page? page = document.GetPage( curPageNum );

                Facility type = PageProcessorFactory.Detect( page );

                IPageProcessor parser = PageProcessorFactory.MakeProcessor( page, type );

                // starting a new patient
                if ( type != Facility.Unknown )
                {
                    // remember the previous patient
                    if ( curPatient != null )
                        patients.Add( curPatient );

                    curPatient = new PatientInfo { Name = parser.ExtractName() };
                }

                // queue this page
                if ( curPatient != null )
                    curPatient.Pages.Enqueue( curPageNum );

                ++curPageNum;

            } while ( curPageNum <= document.NumberOfPages );

            // add the last patient
            if ( curPatient != null )
                patients.Add( curPatient );

            // write out all the facesheets
            patients.ForEach( p => WriteFacesheet( document, p.LastName, p.Pages.ToArray() ) );

            Console.WriteLine( "Stopping..." );

        }

        /// <summary></summary>
        /// <param name="facesheet"></param>
        //public void Run( string facesheet )
        //{
        //    Console.WriteLine( "Starting..." );

        //    // get the environment var
        //    string env = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" ) ?? "Development";

        //    using PdfDocument document = PdfDocument.Open( Path.Combine( BasePath, facesheet ) );

        //    List<PatientInfo> patients = new List<PatientInfo>();

        //    // 1 indexed
        //    int curPageNum = 1;

        //    // hold a ref to the current patient
        //    PatientInfo? curPatient = null;

        //    do
        //    {
        //        // get the current page
        //        Page? page = document.GetPage( curPageNum );

        //        // get all the lines on this page
        //        var pageLines = ExtractPageLines( page );

        //        // does this page start a new patient
        //        PatientInfo nextPatient = ParsePage( pageLines );

        //        // starting a new patient
        //        if ( nextPatient.IsValid )
        //        {
        //            // remember the previous patient
        //            if ( curPatient != null )
        //                patients.Add( curPatient );

        //            curPatient = nextPatient;
        //        }

        //        // queue this page
        //        if ( curPatient != null )
        //            curPatient.Pages.Enqueue( curPageNum );

        //        ++curPageNum;

        //    } while ( curPageNum <= document.NumberOfPages );

        //    // write out all the facesheets
        //    foreach ( PatientInfo p in patients )
        //    {
        //        WriteFacesheet( document, p.LastName, p.Pages.ToArray() );
        //    }

        //    Console.WriteLine( "Stopping..." );
        //}

        /// <summary></summary>
        /// <param name="page"></param>
        /// <returns></returns>
        //public List<string> ExtractPageLines( Page page )
        //{
        //    List<string> lines = new List<string>();
        //    var sb = new StringBuilder();

        //    // 0. Preprocessing
        //    var letters = page.Letters; // no preprocessing

        //    // 1. Extract words
        //    var wordExtractor = NearestNeighbourWordExtractor.Instance;
            
        //    var wordExtractorOptions = new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
        //    {
        //        Filter = ( pivot, candidate ) =>
        //        {
        //            // check if white space (default implementation of 'Filter')
        //            if ( string.IsNullOrWhiteSpace( candidate.Value ) )
        //            {
        //                // pivot and candidate letters cannot belong to the same word 
        //                // if candidate letter is null or white space.
        //                // ('FilterPivot' already checks if the pivot is null or white space by default)
        //                return false;
        //            }

        //            // check for height difference
        //            var maxHeight = Math.Max( pivot.PointSize, candidate.PointSize );
        //            var minHeight = Math.Min( pivot.PointSize, candidate.PointSize );

        //            if ( minHeight != 0 && maxHeight / minHeight > 2.0 )
        //            {
        //                // pivot and candidate letters cannot belong to the same word 
        //                // if one letter is more than twice the size of the other.
        //                return false;
        //            }

        //            // check for colour difference
        //            var pivotRgb = pivot.Color.ToRGBValues();

        //            var candidateRgb = candidate.Color.ToRGBValues();

        //            if ( !pivotRgb.Equals( candidateRgb ) )
        //            {
        //                // pivot and candidate letters cannot belong to the same word 
        //                // if they don't have the same colour.
        //                return false;
        //            }

        //            return true;
        //        }
        //    };

        //    // get all the words using the filters
        //    var words = wordExtractor.GetWords( letters );

        //    // 2. Segment page
        //    var pageSegmenter = DocstrumBoundingBoxes.Instance;
        //    var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { };

        //    IReadOnlyList<TextBlock> textBlocks = pageSegmenter.GetBlocks( words );

        //    // 3. Postprocessing
        //    var readingOrder = UnsupervisedReadingOrderDetector.Instance;
        //    IEnumerable<TextBlock> orderedTextBlocks = readingOrder.Get( textBlocks );

        //    // 4. Extract text
        //    foreach ( var block in orderedTextBlocks )
        //    {
        //        sb.Append( block.Text.Normalize( NormalizationForm.FormKC ) ); // normalise text
        //        sb.AppendLine();

        //        lines.Add( block.Text.Normalize( NormalizationForm.FormKC ) );
        //    }

        //    //return sb.ToString();
        //    return lines;
        //}

        /// <summary></summary>
        /// <param name="body"></param>
        /// <returns></returns>
        //public PatientInfo ParsePage( List<string> lines )
        //{
        //    Regex namePat = new Regex( @"^Patient Name:([-,\w ]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        //    PatientInfo patient = new PatientInfo();

        //    foreach ( string line in lines )
        //    {
        //        Match nameMatch = namePat.Match( line.Trim() );

        //        if ( nameMatch.Success && nameMatch.Groups.Count > 1 )
        //            patient.Name = nameMatch.Groups[1].Value;

        //        if ( line.Contains( "cornerstone", StringComparison.InvariantCultureIgnoreCase ) )
        //            patient.Location = Facility.Cornerstone;
        //        else if ( line.Contains( "memorial", StringComparison.InvariantCultureIgnoreCase ) )
        //            patient.Location = Facility.MemorialHermann;
        //    }

        //    return patient;
        //}

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
