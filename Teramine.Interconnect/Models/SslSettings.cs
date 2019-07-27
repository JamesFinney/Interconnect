#region License
// Copyright (c) 2019 Teramine Ltd
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Teramine.Interconnect.Models
{
    public class SslSettings
    {
        [JsonIgnore]
        public static SslSettings NoSsl { get { return null; } }

        [JsonProperty("certificatePath")]
        public string CertificatePath { get; set; }

        [JsonProperty("certificatePassword")]
        public string CertificatePassword { get; set; }

        [JsonProperty("validateRemoteCertificate")]
        public bool ValidateRemoteCertificate { get; set; }

        [JsonProperty("allowedCertificates")]
        public List<CertificateAttributes> AllowedCertificates { get; set; }

        [JsonProperty("allowedIssuers")]
        public List<CertificateAttributes> AllowedIssuers { get; set; }


        [JsonIgnore]
        public X509Certificate2 Certificate { get; set; }

        [JsonIgnore]
        public CertificateAttributes Attributes
        {
            get
            {
                return Certificate?.GetCertificateAttributes();
            }
        }

        [JsonIgnore]
        public CertificateAttributes IssuerAttributes
        {
            get
            {
                return Certificate?.GetIssuerCertificateAttributes();
            }
        }


        public SslSettings()
        {
            AllowedCertificates = new List<CertificateAttributes>();
            AllowedIssuers = new List<CertificateAttributes>();
        }
    }

    public class CertificateAttributes
    {
        [JsonProperty("cn")]
        public string CommonName { get; set; }

        [JsonProperty("ou")]
        public List<string> OrganisationalUnits { get; set; }

        [JsonProperty("o")]
        public string Organisation { get; set; }

        public CertificateAttributes()
        {
            OrganisationalUnits = new List<string>();
        }

        public int Count
        {
            get
            {
                int total = 0;
                if (!string.IsNullOrEmpty(CommonName))
                    total++;

                total += OrganisationalUnits.Count;

                if (!string.IsNullOrEmpty(Organisation))
                    total++;

                return total;
            }
        }

        public bool MatchesAny(List<CertificateAttributes> whitelist)
        {
            bool isMatch = false;

            foreach (var expected in whitelist)
            {
                var expectedMatches = expected.Count;
                var actualMatches = 0;

                // is there a CN check?
                if (!string.IsNullOrEmpty(expected.CommonName))
                {
                    if (expected.CommonName.Equals(this.CommonName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        actualMatches++;
                    }
                }

                // are there OU checks?
                if (expected.OrganisationalUnits.Count > 0)
                {
                    foreach (var requiredOu in expected.OrganisationalUnits)
                    {
                        foreach (var certOu in this.OrganisationalUnits)
                        {
                            if (requiredOu.Equals(certOu, StringComparison.InvariantCultureIgnoreCase))
                            {
                                actualMatches++;
                                break;
                            }
                        }
                    }
                }

                // is there an O check?
                if (!string.IsNullOrEmpty(expected.Organisation))
                {
                    if (expected.Organisation.Equals(this.Organisation, StringComparison.InvariantCultureIgnoreCase))
                    {
                        actualMatches++;
                    }
                }

                // did it meet all criteria
                if (expectedMatches == actualMatches)
                {
                    isMatch = true;
                    break;
                }
            }

            return isMatch;
        }
    }
}
