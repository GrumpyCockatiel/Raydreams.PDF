using System;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace Raydreams.PDF
{
    /// <summary></summary>
    public static class PDFLogic
    {
        /// <summary></summary>
        public static bool PointInBlock( this TextBlock box, PdfPoint pt )
        {
            if ( pt.X < box.BoundingBox.Left || pt.X > box.BoundingBox.Right )
                return false;

            return ( pt.Y < box.BoundingBox.Bottom || pt.Y > box.BoundingBox.Top );
        }

        /// <summary></summary>
        public static TextBlock? JustUnder( this TextBlock box, List<TextBlock> all )
        {
            all.Remove( box );

            // only those below ordered top to bottom
            all = all.Where( b => b.BoundingBox.Bottom < box.BoundingBox.Bottom && b.BoundingBox.Top < box.BoundingBox.Bottom ).OrderByDescending( b => b.BoundingBox.Top ).ToList();

            TextBlock closets = all.Last();

            foreach ( TextBlock b in all )
            {
                // dont recalc each time - save
                double c = box.BoundingBox.TopLeft.GetDistance( closets.BoundingBox.TopLeft );

                double n = box.BoundingBox.TopLeft.GetDistance( b.BoundingBox.TopLeft );

                if ( n < c )
                    closets = b;
            }

            return closets;
        }

        /// <summary></summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public static double GetDistance( this PdfPoint pt1, PdfPoint pt2 ) => Math.Sqrt( Math.Pow( Math.Abs( pt2.X - pt1.X ), 2.0 ) + Math.Pow( Math.Abs( pt2.Y - pt1.Y ), 2.0 ) );
    }
}

