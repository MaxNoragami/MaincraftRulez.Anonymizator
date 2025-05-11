namespace MaincraftRulez.Anonymizator;

public static class Constants
{
    public static object Constants { get; internal set; }

    // Alphabets for different data types
    public static class Alphabets
    {
        public const string Digits = "0123456789";
        public const string LowerAlpha = "abcdefghijklmnopqrstuvwxyz";
        public const string UpperAlpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string AlphaNumeric = Digits + LowerAlpha + UpperAlpha;
        public const string Email = AlphaNumeric + "._-";
        public const string Base64 = AlphaNumeric + "+/=";
        public const string Hex = "0123456789abcdef";
        
        // Extended alphabet with Romanian characters
        public const string ExtendedLatin = AlphaNumeric + "ăĂîÎâÂșȘțȚüÜöÖäÄéÉèÈêÊëËàÀáÁíÍìÌñÑçÇ";
    }

    // Format patterns
    public static class Patterns
    {
        // Existing patterns
        public const string EmailPattern = @"^(.+)@(.+)$";
        public const string PhonePattern = @"^\+?(\d{1,3})?[-.\s]?(\d{1,4})[-.\s]?(\d+)$";
        public const string CCPattern = @"^(\d{4})[-\s]?(\d{4})[-\s]?(\d{4})[-\s]?(\d{4})$";
        
        // New patterns
        public const string DatePattern = @"^(\d{4})-(\d{2})-(\d{2})$"; // ISO format date
        public const string DecimalPattern = @"^([+-]?)(\d+)\.?(\d*)([eE][+-]?\d+)?$"; // Decimal number
    }

    // Tweak constants
    public static class Tweaks
    {
        public static readonly byte[] PhoneTweak = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD };
        public static readonly byte[] EmailTweak = new byte[] { 0xA1, 0xB2, 0xC3, 0xD4, 0xE5, 0xF6, 0x78 };
        public static readonly byte[] NameTweak = new byte[] { 0x98, 0x76, 0x54, 0x32, 0x10, 0xFE, 0xDC };
        public static readonly byte[] IdentifierTweak = new byte[] { 0x24, 0x68, 0xAC, 0xE0, 0x13, 0x57, 0x9B };
        public static readonly byte[] CreditCardTweak = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };
        public static readonly byte[] DefaultTweak = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    }
}