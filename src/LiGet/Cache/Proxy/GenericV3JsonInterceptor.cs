using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace LiGet.Cache.Proxy
{
    public class GenericV3JsonInterceptor : IV3JsonInterceptor
    {
        public void Intercept(Dictionary<string, string> valueReplacements, Stream input, Stream output) {
            var jsonReader = new JsonTextReader(new StreamReader(input));
            var jsonWriter = new JsonTextWriter(new StreamWriter(output, new UTF8Encoding(false)));
            while(jsonReader.Read()) {
                if(jsonReader.TokenType == JsonToken.String) {
                    string value = jsonReader.Value as string;
                    foreach(var kv in valueReplacements) {
                        value = value.Replace(kv.Key, kv.Value);
                    }
                    jsonWriter.WriteToken(JsonToken.String, value);
                }
                else 
                {
                    jsonWriter.WriteToken(jsonReader.TokenType,jsonReader.Value);
                }
            }
            jsonWriter.Flush();
        }
    }
}