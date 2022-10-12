using System;

namespace Raydreams.PDF
{
    public enum Facility
    {
        Unknown = 0,
        MemorialHermann = 1,
        Cornerstone = 2
    }

    public class PatientInfo
    {
        /// <summary></summary>
        public string Name
        {
            set
            {
                if ( String.IsNullOrWhiteSpace( value ) )
                    return;

                var parts = value.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );

                if ( parts.Length < 1 )
                    return;

                this.LastName = parts[0];

                if ( parts.Length > 1 )
                    this.FirstName = parts[1];
            }
        }

        public bool IsValid => !String.IsNullOrWhiteSpace( this.LastName ) && this.Location != Facility.Unknown;

        /// <summary></summary>
        public string FirstName { get; set; } = String.Empty;

        /// <summary></summary>
        public string LastName { get; set; } = String.Empty;

        /// <summary></summary>
        public DateTimeOffset? DOB { get; set; } = null;

        /// <summary></summary>
        public Facility Location { get; set; } = Facility.Unknown;

        /// <summary></summary>
        public Queue<int> Pages { get; set; } = new Queue<int>();
    }
}

