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

        public static readonly string[] Facesheets = new string[] { "Facesheets-5.pdf" };

        /// <summary>Main entry class</summary>
        /// <param name="args">any future command line args</param>
        /// <returns>exit value</returns>
        public static int Main( string[] args )
        {
            // get the environment var
            string env = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" ) ?? "Development";

            Program app = new Program();

            Console.WriteLine( "Starting..." );

            foreach ( string facesheet in Facesheets )
                app.Run( facesheet );

            Console.WriteLine( "Stopping..." );

            return 0;
        }

        /// <summary></summary>
        /// <param name="facesheet"></param>
        public int Run( string facesheet )
        {
            FileInfo path = new FileInfo( Path.Combine( BasePath, facesheet ) );
            if ( !path.Exists )
                return 0;

            using PdfDocument document = PdfDocument.Open( path.FullName );

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

                // detected a new facesheet
                if ( type != Facility.Unknown )
                {
                    // remember the previous patient
                    if ( curPatient != null )
                        patients.Add( curPatient );

                    curPatient = new PatientInfo { Name = parser.ExtractName(), DOB = parser.ExtractDOB(), Location = type  };
                    _ = 5;
                }
                else if ( curPageNum < 2)
                {
                    // the first page was NOT a starting page
                    Console.WriteLine( "Page 1 was not the start of a Facesheet." );
                    return 0;
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
            int writes = 0;
            patients.ForEach( (p) => { WriteFacesheet( document, p.LastName, p.Pages.ToArray() ); ++writes; } );
            return writes;
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
