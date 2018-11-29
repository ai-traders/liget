namespace BaGet.Core.Legacy.OData
{
    public class ODataResponse<T>
    {
        private readonly string serviceBaseUrl;
        private readonly T entity;
        public ODataResponse(string serviceBaseUrl, T entity)
        {
            this.serviceBaseUrl = serviceBaseUrl;
            this.entity = entity;
        }

        public string ServiceBaseUrl => serviceBaseUrl;

        public T Entity => entity;
    }
}
