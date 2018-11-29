using System;

namespace BaGet.Core.Legacy
{
    public class Constants
    {
        public const string HashAlgorithm = "SHA512";
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
    }
}