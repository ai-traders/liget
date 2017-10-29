namespace LiGet
{
    [System.Serializable]
    public class PackageDuplicateException : System.Exception
    {
        public PackageDuplicateException() { }
        public PackageDuplicateException(string message) : base(message) { }
        public PackageDuplicateException(string message, System.Exception inner) : base(message, inner) { }
        protected PackageDuplicateException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}