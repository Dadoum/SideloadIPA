using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Threading.Tasks;
using SecureRemotePassword;

namespace SideloadIPA
{
    class CertificateRequest
    {
        public static readonly CookieContainer Cookies = new CookieContainer();

        public static readonly HttpClient Client = new HttpClient(new HttpClientHandler()
        {
            CookieContainer = Cookies,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // THIS GITHUB ISSUE THREAD IS LITERALLY GOLD: https://github.com/horrorho/InflatableDonkey/issues/87 GIVE THEM LOVE, ESPECIALLY TO RobLinux
        public static async Task Main(string[] args)
        {
            // Apple ID for testing, use temp-mail.org to login.
            var email = "compte.adam.c@gmail.com";
            var password = "Newmail90";

            Cookies.Add(new Cookie("dslang", "US-EN", "/", "gsa.apple.com"));
            ServicePointManager
                    .ServerCertificateValidationCallback =
                (sender, cert, chain, sslPolicyErrors) => true;

            // Initiate SRP Client, and generate ephemeral keys
            var srpClient = new SrpClient();
            var eph = srpClient.GenerateEphemeral();
            
            // Split keys into lines
            var splitPublic = "\t\t" + string.Join("\r\n\t\t", SplitInParts(ToB64(eph.Public), 60))  + "\r\n";

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
                    "\t\t\t<key>X-Apple-I-SRL-NO</key>\r\n",
                    "\t\t\t<string>DNPS88X1HG82</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD</key>\r\n",
                    "\t\t\t<string>AAAABQAAABCZg6d+upDLWkNW98xGuiOXAAAAAw==</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-M</key>\r\n",
                    "\t\t\t<string>faFsm6glupJEkv10H8HxT+cpTVQ/q8mWyPiJRkYfbXJDfIK+eL52Z7GyRCamzWYnvv1N/US6VaawwZG9</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-RINFO</key>\r\n",
                    "\t\t\t<string>17106176</string>\r\n",
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
                splitPublic);
            
            Console.WriteLine(post1);
            var res1 = PList.ParsePList(new PList(await PostRequest("https://gsa.apple.com/grandslam/GsService2", post1)));

            Console.WriteLine(res1);
            
            // c is a string in the form x-xxx-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx:AAA with x lower case hexadecimal numbers and A letters in upper case 
            var c = res1["Response"]["c"].ToString();
            
            // Convert in hexa for SRP.NET
            var b =  new BigInteger(Convert.FromBase64String(res1["Response"]["B"].ToString())).ToString("X");
            var salt = new BigInteger(Convert.FromBase64String(res1["Response"]["s"].ToString())).ToString("X");
            
            // Generate keys for Apple SRP protocol
            var pk = srpClient.DerivePrivateKey(salt, email, password);
            var clientSession = srpClient.DeriveSession(eph.Secret, 
                b, salt, email, pk);
            
            // POST request to Apple, but it fails 'Your Apple ID or password is incorrect'
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
                    "\t\t\t<key>X-Apple-I-SRL-NO</key>\r\n",
                    "\t\t\t<string>DNPS88X1HG82</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD</key>\r\n",
                    "\t\t\t<string>AAAABQAAABCZg6d+upDLWkNW98xGuiOXAAAAAw==</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-M</key>\r\n",
                    "\t\t\t<string>faFsm6glupJEkv10H8HxT+cpTVQ/q8mWyPiJRkYfbXJDfIK+eL52Z7GyRCamzWYnvv1N/US6VaawwZG9</string>\r\n",
                    "\t\t\t<key>X-Apple-I-MD-RINFO</key>\r\n",
                    "\t\t\t<string>17106176</string>\r\n",
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
            // Setting content of request
            var content = new HttpRequestMessage
            {
                // Requests for auth are made to gsa.apple.com 
                RequestUri = new Uri(url),
                // Posting
                Method = HttpMethod.Post,
                // Headers are from ReProvision from Matchstic
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
                // Plist as content
                Content = new StringContent(request)
            };
            // Set user agent
            Client.DefaultRequestHeaders.Add("User-Agent", "ReProvision/1 CFNetwork/978.0.7 Darwin/18.7.0");
            Client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Post request, getting answer, and convert it to easily parsable Newtonsoft.Json's JObject
            var response = await Client.SendAsync(content);
            var responseString = await response.Content.ReadAsStringAsync();
            var handle = PList.ParsePList(new PList(responseString));

            // Checking if Error Code is equals to 0
            if (handle["Response"]["Status"]["ec"].ToString() != "0")
                throw new AppleException(handle["Response"]["Status"]);


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