using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace Raydreams.PDF
{
    /// <summary></summary>
    public static class PDFLogic
    {
        /// <summary>Is the given point inside the text block</summary>
        public static bool PointInBlock( this TextBlock box, PdfPoint pt )
        {
            if ( pt.X < box.BoundingBox.Left || pt.X > box.BoundingBox.Right )
                return false;

            return ( pt.Y < box.BoundingBox.Bottom || pt.Y > box.BoundingBox.Top );
        }

        /// <summary>Find the text block that is most likely to be just below and left justified</summary>
        public static TextBlock? JustUnder( this TextBlock box, List<TextBlock> all )
        {
            all.Remove( box );

            // only those below ordered top to bottom
            all = all.Where( b => b.BoundingBox.Bottom < box.BoundingBox.Bottom && b.BoundingBox.Top < box.BoundingBox.Bottom ).OrderByDescending( b => b.BoundingBox.Top ).ToList();

            // init with a block that's far away
            ( TextBlock Block, double Dist) closest = ( all.Last(), box.BoundingBox.TopLeft.GetDistance( all.Last().BoundingBox.TopLeft ) );

            foreach ( TextBlock b in all )
            {
                // distance to box we are testing
                double n = box.BoundingBox.TopLeft.GetDistance( b.BoundingBox.TopLeft );

                if ( n < closest.Dist )
                    closest = ( b, n);
            }

            return closest.Block;
        }

        /// <summary>Get the distance between two double points</summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public static double GetDistance( this PdfPoint pt1, PdfPoint pt2 ) => Math.Sqrt( Math.Pow( Math.Abs( pt2.X - pt1.X ), 2.0 ) + Math.Pow( Math.Abs( pt2.Y - pt1.Y ), 2.0 ) );
    }
}

