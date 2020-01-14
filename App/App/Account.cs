using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;

namespace PortfolioPicker.App
{
    public class Account
    {
        public string Name { get; set; }

        public string Brokerage { get; set; }

        public AccountType Type { get; set; } = AccountType.BROKERAGE;

        [YamlIgnore]
        private IList<Position> _positions;
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
                Value = _positions.Sum(x => x.Value);

                // compute their exposures
                Exposures = new List<Exposure>();
                foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                    foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                        Exposures.Add(new Exposure(c, l));

                // calculate each position's contributions to various exposures of interest
                foreach (var p in _positions)
                {
                    var fund = Fund.Get(p.Symbol);
                    foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                    {
                        foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                        {
                            var e = Exposures.First(x => x.Class == c && x.Location == l);
                            e.Value += (double)p.Value * fund.Ratio(c) * fund.Ratio(l);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// sum of values of all positions
        /// </summary>
        [YamlIgnore]
        internal decimal Value { get; private set; }

        [YamlIgnore]
        public IList<Exposure> Exposures {get; private set;}

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