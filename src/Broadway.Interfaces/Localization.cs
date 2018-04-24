using System;

namespace NuClear.Broadway.Interfaces
{
    public struct Localization : IEquatable<Localization>
    {
        public Language Lang { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }

        public bool Equals(Localization other)
        {
            return Lang == other.Lang &&
                   Name.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase) &&
                   ShortName.Equals(other.ShortName, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static bool operator ==(Localization left, Localization right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Localization left, Localization right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj) => obj is Localization localization && Equals(localization);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Lang != null ? Lang.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ShortName != null ? ShortName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}