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
        public AccountType Type { get; set; } = AccountType.TAXABLE;

        [DataMember(IsRequired = true)]
        public IList<Position> Positions 
        {
            get
            {
                return _positions;
            }
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
                Name = this.Name,
                Brokerage = this.Brokerage,
                Type = this.Type,
            };
        }

        public bool Equals(Account rhs)
        {
            if (rhs is null)
                return false;

            if (ReferenceEquals(this, rhs))
                return true;

            return this.Name == rhs.Name
                && this.Brokerage == rhs.Brokerage
                && this.Type == rhs.Type;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Account);
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