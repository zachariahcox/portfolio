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
                if (value != null)
                {
                    _value = value.Sum(x => x.Value);
                }
            }
        }

        [IgnoreDataMember]
        internal decimal Value => _value;

        [IgnoreDataMember]
        private decimal _value;

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