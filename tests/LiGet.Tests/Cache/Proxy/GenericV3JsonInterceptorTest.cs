using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LiGet.Cache.Proxy;
using Xunit;

namespace LiGet.Tests.Cache.Proxy
{
    public class GenericV3JsonInterceptorTest
    {
        private GenericV3JsonInterceptor interceptor;
        Dictionary<string, string> replacements = new Dictionary<string, string>() { 
                { "https://api.nuget.org/v3", "http://liget.com:9011/api/cache/v3" } 
            };

        public GenericV3JsonInterceptorTest() {
            interceptor = new GenericV3JsonInterceptor();
        }

        [Fact]
        public void ShouldReplaceIndexEntryContent() {
            string input = @"{
                ""@id"": ""https://api.nuget.org/v3/registration3/"",
                ""@type"": ""RegistrationsBaseUrl""
                }";
            using(var istream = AsStream(input)) {
                using(var ostream = new MemoryStream())
                {
                    interceptor.Intercept(replacements, istream, ostream);
                    string output = AsString(ostream);
                    Assert.Equal(@"{""@id"":""http://liget.com:9011/api/cache/v3/registration3/"",""@type"":""RegistrationsBaseUrl""}", output);
                }
            }
        }

        [Fact]
        public void ShouldReplaceFlatContainerEntryContent() {
            string input = @"{
                ""@id"": ""https://api.nuget.org/v3-flatcontainer/"",
                ""@type"": ""PackageBaseAddress/3.0.0""
                }";
            using(var istream = AsStream(input)) {
                using(var ostream = new MemoryStream())
                {
                    interceptor.Intercept(replacements, istream, ostream);
                    string output = AsString(ostream);
                    Assert.Equal(@"{""@id"":""http://liget.com:9011/api/cache/v3-flatcontainer/"",""@type"":""PackageBaseAddress/3.0.0""}", output);
                }
            }
        }

        private static string AsString(MemoryStream ostream)
        {
            return Encoding.UTF8.GetString(ostream.ToArray());
        }

        private Stream AsStream(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }
    }
}