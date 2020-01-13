using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Account
    {
        [IgnoreDataMember]
        private IList<Position> _positions;

        [DataMember(IsRequired = true)]
        public string Name { get; set; }

        [DataMember(IsRequired = true)]
        public string Brokerage { get; set; }

        [DataMember(IsRequired = true)]
        public AccountType Type { get; set; } = AccountType.BROKERAGE;

        [DataMember(IsRequired = true)]
        public IList<Position> Positions
        {
            get => _positions;
            set
            {
                _positions = value;
                if (value is null)
                {
                    return;
                }

                // check for accidental duplicate positions
                var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicatedSymbols = new List<string>();
                foreach(var p in _positions)
                {
                    if (dedup.Contains(p.Symbol))
                    {
                        duplicatedSymbols.Add(p.Symbol);
                    }
                    else
                    {
                        dedup.Add(p.Symbol);
                    }
                }
                if (duplicatedSymbols.Count > 0)
                {
                    throw new ArgumentException($"Invalid portfolio: \"{this.Name}\" has multiple positions for {string.Join(", ", duplicatedSymbols)}");
                }
                
                // positions are ok, sum up thier total value
                Value = value.Sum(x => x.Value);
            }
        }

        /// <summary>
        /// sum of values of all positions
        /// </summary>
        [IgnoreDataMember]
        internal decimal Value { get; private set; }

        public override string ToString() => $"{Name}@{Brokerage}";

        internal Account Clone()
        {
            return new Account
            {
                Name = Name,
                Brokerage = Brokerage,
                Type = Type,
            };
        }

        public bool Equals(Account rhs)
        {
            if (rhs is null)
            {
                return false;
            }

            if (ReferenceEquals(this, rhs))
            {
                return true;
            }

            return Name == rhs.Name
                && Brokerage == rhs.Brokerage
                && Type == rhs.Type;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Account);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Brokerage, Type);
        }

        public static bool operator ==(Account lhs, Account rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Account lhs, Account rhs)
        {
            return !(lhs == rhs);
        }
    }
}