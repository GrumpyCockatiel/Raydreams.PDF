using System;

namespace Raydreams.PDF
{
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

        /// <summary></summary>
        public string FirstName { get; set; } = String.Empty;

        /// <summary></summary>
        public string LastName { get; set; } = String.Empty;

        /// <summary></summary>
        public DateTimeOffset? DOB { get; set; } = null;

        public Queue<int> Pages { get; set; } = new Queue<int>();
    }
}

