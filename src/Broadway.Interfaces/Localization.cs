using System;

namespace NuClear.Broadway.Interfaces
{
    public struct Localization : IEquatable<Localization>
    {
        public Localization(string lang, string name, string shortName = null)
        {
            Lang = lang;
            Name = name;
            ShortName = shortName;
        }
        
        public string Lang { get; }
        public string Name { get; }
        public string ShortName { get; }

        public bool Equals(Localization other)
        {
            return (Lang?.Equals(other.Lang, StringComparison.InvariantCultureIgnoreCase) ?? other.Lang == null) &&
                   (Name?.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase) ?? other.Name == null);
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
                var hashCode = Lang != null ? Lang.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}