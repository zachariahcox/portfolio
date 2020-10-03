using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Account
    {
        [DataMember(IsRequired=true)]
        public string Name { 
            get => _name; 
            set => _name = value is null ? null : value.ToLower().Trim(); 
            }

        [DataMember(IsRequired=true)]
        public string Brokerage { 
            get => _brokerage; 
            set => _brokerage = value is null ? null : value.ToLower().Trim(); 
            }

        [DataMember(IsRequired=true)]
        public AccountType Type { get; set; } = AccountType.BROKERAGE;

        [DataMember(IsRequired=true)]
        public IList<Position> Positions
        {
            get => _positions;
            set
            {
                _positions = value;
                _exposures = null;
                if (value is null || value.Count == 0)
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
            }
        }
        
        internal double Value => Positions.Sum(x => x.Value);

        public IList<Exposure> GetExposures(SecurityCache sc)
        {
            if (_exposures is null) 
            {
                // calculate each position's contributions to various exposures of interest
                var es = new List<Exposure>();
                foreach (var p in _positions)
                {
                    var fund = sc.Get(p.Symbol);
                    foreach (var c in AssetClasses.ALL)
                    foreach (var l in AssetLocations.ALL)
                    {
                        if (c == AssetClass.None || l == AssetLocation.None)
                            continue;
                            
                        var e = es.FirstOrDefault(x => x.Class == c && x.Location == l);
                        if (e is null)
                        {
                            e = new Exposure(c, l);
                            es.Add(e);
                        }

                        // increase exposure
                        e.Value += p.Value * fund.Ratio(c) * fund.Ratio(l);
                    }
                }
                _exposures = es;
            }
            return _exposures;
        }
         
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
    
        [IgnoreDataMember]
        private string _name;
        [IgnoreDataMember]
        private string _brokerage;
        [IgnoreDataMember]
        private IList<Position> _positions;
        [IgnoreDataMember]
        private IList<Exposure> _exposures;
    }
}