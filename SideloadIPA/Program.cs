using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using SecureRemotePassword;

namespace SideloadIPA
{
    class CertificateRequest
    {
        public static readonly CookieContainer cookies = new CookieContainer();

        public static readonly HttpClient client = new HttpClient(new HttpClientHandler()
        {
            CookieContainer = cookies,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        public bool CheckValidationResult(ServicePoint sp,
            X509Certificate certificate, WebRequest request, int error)
        {
            return true;
        }


        public static async Task Main(string[] args)
        {
            // Apple ID for testing, use temp-mail.org to login.
            var email = "hakaf67615@newmail.top";
            var password = "Newmail90";

            cookies.Add(new Cookie("dslang", "US-EN", "/", "gsa.apple.com"));
            ServicePointManager
                    .ServerCertificateValidationCallback =
                (sender, cert, chain, sslPolicyErrors) => true;

            var client = new SrpClient();
            var eph = client.GenerateEphemeral();
            
            var key2send = "\t\t" + string.Join("\r\n\t\t", SplitInParts(ToB64(eph.Public), 60))  + "\r\n";
            
            var post1 = string.Format(string.Concat(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n",
                    "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\r\n",
                    "<plist version=\"1.0\">\r\n",
                    "<dict>\r\n",
                    "\t<key>Header</key>\r\n",
                    "\t<dict>\r\n",
                    "\t\t<key>Version</key>\r\n",
                    "\t\t<string>1.0.1</string>\r\n",
                    "\t</dict>\r\n",
                    "\t<key>Request</key>\r\n",
                    "\t<dict>\r\n",
                    "\t\t<key>A2k</key>\r\n",
                    "\t\t<data>\r\n",
                    "{1}",
                    "\t\t</data>\r\n",
                    "\t\t<key>cpd</key>\r\n",
                    "\t\t<dict>\r\n",
                    "\t\t\t<key>X-Apple-I-Client-Time</key>\r\n",
                    "\t\t\t<string>2019-11-30T22:10:15Z</string>\r\n",
                    "\t\t\t<key>X-Apple-I-Locale</key>\r\n",
                    "\t\t\t<string>en_FR</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD</key>\r\n",
                    "\t\t\t<string>AAAABQAAABCZg6d+upDLWkNW98xGuiOXAAAAAw==</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-M</key>\r\n",
                    "\t\t\t<string>faFsm6glupJEkv10H8HxT+cpTVQ/q8mWyPiJRkYfbXJDfIK+eL52Z7GyRCamzWYnvv1N/US6VaawwZG9</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-RINFO</key>\r\n",
                    "\t\t\t<string>17106176</string>\r\n",
                    "\t\t\t<key>X-Apple-I-SRL-NO</key>\r\n",
                    "\t\t\t<string>DNPS88X1HG82</string>\r\n",
                    "\t\t\t<key>X-Apple-I-TimeZone</key>\r\n",
                    "\t\t\t<string>CET</string>\r\n",
                    "\t\t\t<key>X-Apple-Locale</key>\r\n",
                    "\t\t\t<string>en_FR</string>\r\n",
                    "\t\t\t<key>bootstrap</key>\r\n",
                    "\t\t\t<true/>\r\n",
                    "\t\t\t<key>icscrec</key>\r\n",
                    "\t\t\t<true/>\r\n",
                    "\t\t\t<key>loc</key>\r\n",
                    "\t\t\t<string>en_FR</string>\r\n",
                    "\t\t\t<key>pbe</key>\r\n",
                    "\t\t\t<false/>\r\n",
                    "\t\t\t<key>prkgen</key>\r\n",
                    "\t\t\t<true/>\r\n",
                    "\t\t\t<key>svct</key>\r\n",
                    "\t\t\t<string>iCloud</string>\r\n",
                    "\t\t</dict>\r\n",
                    "\t\t<key>o</key>\r\n",
                    "\t\t<string>init</string>\r\n",
                    "\t\t<key>ps</key>\r\n",
                    "\t\t<array>\r\n",
                    "\t\t\t<string>s2k</string>\r\n",
                    "\t\t\t<string>s2k_fo</string>\r\n",
                    "\t\t</array>\r\n",
                    "\t\t<key>u</key>\r\n",
                    "\t\t<string>{0}</string>\r\n",
                    "\t</dict>\r\n",
                    "</dict>\r\n",
                    "</plist>\r\n"
                ), email,
                key2send);
            
            Console.WriteLine(post1);
            var res1 = PList.ParsePList(new PList(await PostRequest("https://gsa.apple.com/grandslam/GsService2", post1)));

            Console.WriteLine(res1);

            var c = res1["Response"]["c"].ToString();
            var b =  new BigInteger(Convert.FromBase64String(res1["Response"]["B"].ToString())).ToString("X");
            var salt =  new BigInteger(Convert.FromBase64String(res1["Response"]["s"].ToString())).ToString("X");
            
            var pk = client.DerivePrivateKey(salt, email, password);
            var clientSession = client.DeriveSession(eph.Secret, 
                b, salt, email, pk);


            var post2 = string.Format(string.Concat(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n",
                    "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\r\n",
                    "<plist version=\"1.0\">\r\n",
                    "<dict>\r\n",
                    "\t<key>Header</key>\r\n",
                    "\t<dict>\r\n",
                    "\t\t<key>Version</key>\r\n",
                    "\t\t<string>1.0.1</string>\r\n",
                    "\t</dict>\r\n",
                    "\t<key>Request</key>\r\n",
                    "\t<dict>\r\n",
                    "\t\t<key>M1</key>\r\n",
                    "\t\t<data>\r\n",
                    "\t\t{2}\r\n",
                    "\t\t</data>\r\n",
                    "\t\t<key>c</key>\r\n",
                    "\t\t<string>{1}</string>\r\n",
                    "\t\t<key>cpd</key>\r\n",
                    "\t\t<dict>\r\n",
                    "\t\t\t<key>X-Apple-I-Client-Time</key>\r\n",
                    "\t\t\t<string>2019-11-30T22:10:15Z</string>\r\n",
                    "\t\t\t<key>X-Apple-I-Locale</key>\r\n",
                    "\t\t\t<string>en_FR</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD</key>\r\n",
                    "\t\t\t<string>AAAABQAAABCZg6d+upDLWkNW98xGuiOXAAAAAw==</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-M</key>\r\n",
                    "\t\t\t<string>faFsm6glupJEkv10H8HxT+cpTVQ/q8mWyPiJRkYfbXJDfIK+eL52Z7GyRCamzWYnvv1N/US6VaawwZG9</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-RINFO</key>\r\n",
                    "\t\t\t<string>17106176</string>\r\n",
                    "\t\t\t<key>X-Apple-I-SRL-NO</key>\r\n",
                    "\t\t\t<string>DNPS88X1HG82</string>\r\n",
                    "\t\t\t<key>X-Apple-I-TimeZone</key>\r\n",
                    "\t\t\t<string>CET</string>\r\n",
                    "\t\t\t<key>X-Apple-Locale</key>\r\n",
                    "\t\t\t<string>en_FR</string>\r\n",
                    "\t\t\t<key>bootstrap</key>\r\n",
                    "\t\t\t<true/>\r\n",
                    "\t\t\t<key>icscrec</key>\r\n",
                    "\t\t\t<true/>\r\n",
                    "\t\t\t<key>loc</key>\r\n",
                    "\t\t\t<string>en_FR</string>\r\n",
                    "\t\t\t<key>pbe</key>\r\n",
                    "\t\t\t<false/>\r\n",
                    "\t\t\t<key>prkgen</key>\r\n",
                    "\t\t\t<true/>\r\n",
                    "\t\t\t<key>svct</key>\r\n",
                    "\t\t\t<string>iCloud</string>\r\n",
                    "\t\t</dict>\r\n",
                    "\t\t<key>o</key>\r\n",
                    "\t\t<string>complete</string>\r\n",
                    "\t\t<key>u</key>\r\n",
                    "\t\t<string>{0}</string>\r\n",
                    "\t</dict>\r\n",
                    "</dict>\r\n",
                    "</plist>\r\n"
                ), email,
                c,
                ToB64(clientSession.Proof));
            
            Console.WriteLine(post2);
            var res2 = PList.ParsePList(new PList(await PostRequest("https://gsa.apple.com/grandslam/GsService2", post2)));

            Console.WriteLine(res2);

            Console.ReadKey();

        }

        public static async Task<string> PostRequest(string url, string request)
        {
            var _content = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                {
                    {
                        "X-MMe-Client-Info",
                        "<MacBookPro11,5> <Mac OS X;10.14.6;18G103> <com.apple.AuthKit/1 (com.apple.akd/1.0)>"
                    },
                    {"Accept-Language", "en-us"},
                    {"Accept-Encoding", "br, gzip, deflate"},
                    {"Connection", "keep-alive"},
                    {"Accept-Language", "en-us"},
                    {"User-Agent", "ReProvision/1 CFNetwork/978.0.7 Darwin/18.7.0"}
                },
                //Content = ((HttpContent) new StringContent(s, Encoding.UTF8, "application/json")),
                Content = new StringContent(request)
            };
            client.DefaultRequestHeaders.Add("User-Agent", "ReProvision/1 CFNetwork/978.0.7 Darwin/18.7.0");
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.SendAsync(_content);
            var responseString = await response.Content.ReadAsStringAsync();
            // Console.WriteLine(responseString);
            var handle = PList.ParsePList(new PList(responseString));

            if (handle["Response"]["Status"]["ec"].ToString() != "0")
                throw new AppleException(handle["Response"]["Status"]);

            // Console.WriteLine(responseString);

            return responseString;
        }

        public static IEnumerable<String> SplitInParts(String s, Int32 partLength)
        {
            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        public static string ToB64(string hex) => Convert.ToBase64String(SrpInteger.FromHex(hex).ToByteArray());
    }
}