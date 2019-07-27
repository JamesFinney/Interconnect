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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Teramine.Interconnect.Models;

namespace Teramine.Interconnect.Test
{
    [TestFixture]
    public class SecurityTests
    {
        [Test]
        public void Extensions_ParseCertificateAttributesString_AttributePresence()
        {
            // regular - mix of CNs, OU, and O numbers
            var test0 = "CN=test_subject, OU=test_ou_0, OU=test_ou_1, O=organisation";
            var test1 = "CN=test_subject, O=organisation";
            var test2 = "CN=test_subject, OU=test_ou_0, OU=test_ou_1";
            var test3 = "OU=test_ou_0, OU=test_ou_1, O=organisation";
            var test4 = "O=organisation";
            var test5 = string.Empty;

            {
                var result = Extensions.ParseCertificateAttributesString(test0);
                Assert.AreEqual("test_subject", result.CommonName);
                Assert.AreEqual(2, result.OrganisationalUnits.Count);
                Assert.AreEqual("test_ou_0", result.OrganisationalUnits[0]);
                Assert.AreEqual("test_ou_1", result.OrganisationalUnits[1]);
                Assert.AreEqual("organisation", result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test1);
                Assert.AreEqual("test_subject", result.CommonName);
                Assert.AreEqual(0, result.OrganisationalUnits.Count);
                Assert.AreEqual("organisation", result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test2);
                Assert.AreEqual("test_subject", result.CommonName);
                Assert.AreEqual(2, result.OrganisationalUnits.Count);
                Assert.AreEqual("test_ou_0", result.OrganisationalUnits[0]);
                Assert.AreEqual("test_ou_1", result.OrganisationalUnits[1]);
                Assert.IsNull(result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test3);
                Assert.IsNull(result.CommonName);
                Assert.AreEqual(2, result.OrganisationalUnits.Count);
                Assert.AreEqual("test_ou_0", result.OrganisationalUnits[0]);
                Assert.AreEqual("test_ou_1", result.OrganisationalUnits[1]);
                Assert.AreEqual("organisation", result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test4);
                Assert.IsNull(result.CommonName);
                Assert.AreEqual(0, result.OrganisationalUnits.Count);
                Assert.AreEqual("organisation", result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test5);
                Assert.IsNull(result.CommonName);
                Assert.AreEqual(0, result.OrganisationalUnits.Count);
                Assert.IsNull(result.Organisation);
            }
        }

        [Test]
        public void Extensions_ParseCertificateAttributesString_Spacing()
        {
            // regular - mix of CNs, OU, and O numbers
            var test0 = "CN=   test_subject   , OU=test_ou_0 , OU=   test_ou_1  , O= organisation   ";
            var test1 = "CN=test subject   ,OU=test ou 0 , OU=test ou   1  , O=         tabbed my organisation   ";
            var test2 = "CN=    test subject   ,OU=    test ou 0 , OU=    test ou   1  , O=   my organisation   ";

            {
                var result = Extensions.ParseCertificateAttributesString(test0);
                Assert.AreEqual("test_subject", result.CommonName);
                Assert.AreEqual(2, result.OrganisationalUnits.Count);
                Assert.AreEqual("test_ou_0", result.OrganisationalUnits[0]);
                Assert.AreEqual("test_ou_1", result.OrganisationalUnits[1]);
                Assert.AreEqual("organisation", result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test1);
                Assert.AreEqual("test subject", result.CommonName);
                Assert.AreEqual(2, result.OrganisationalUnits.Count);
                Assert.AreEqual("test ou 0", result.OrganisationalUnits[0]);
                Assert.AreEqual("test ou   1", result.OrganisationalUnits[1]);
                Assert.AreEqual("tabbed my organisation", result.Organisation);
            }

            {
                var result = Extensions.ParseCertificateAttributesString(test2);
                Assert.AreEqual("test subject", result.CommonName);
                Assert.AreEqual(2, result.OrganisationalUnits.Count);
                Assert.AreEqual("test ou 0", result.OrganisationalUnits[0]);
                Assert.AreEqual("test ou   1", result.OrganisationalUnits[1]);
                Assert.AreEqual("my organisation", result.Organisation);
            }
        }

        [Test]
        public void CertificateAttribute_Count()
        {
            {
                var test = new CertificateAttributes
                {
                    CommonName = "CommonName",
                    OrganisationalUnits = new List<string>
                    {
                        "OU0",
                        "OU1",
                        "OU2"
                    },
                    Organisation = "Organisation"
                };
                Assert.AreEqual(5, test.Count);
            }

            {
                var test = new CertificateAttributes
                {
                    OrganisationalUnits = new List<string>
                    {
                        "OU0",
                        "OU1",
                        "OU2"
                    },
                    Organisation = "Organisation"
                };
                Assert.AreEqual(4, test.Count);
            }

            {
                var test = new CertificateAttributes
                {
                    Organisation = "Organisation"
                };
                Assert.AreEqual(1, test.Count);
            }

            {
                var test = new CertificateAttributes
                {
                    CommonName = "CommonName",
                    Organisation = "Organisation"
                };
                Assert.AreEqual(2, test.Count);
            }
        }

        [Test]
        public void CertificateAttribute_MatchesAny()
        {
            #region Passes

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string> { "OU0", "OU1", "OU2" },
                        Organisation = "Organisation"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU0",
                            "OU1",
                            "OU2"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsTrue(baseCert.MatchesAny(whitelist));
                }
            }

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string> { "OU0" },
                        Organisation = "Organisation"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU0",
                            "OU1",
                            "OU2"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsTrue(baseCert.MatchesAny(whitelist));
                }
            }

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        CommonName = "CommonName"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU0",
                            "OU1",
                            "OU2"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsTrue(baseCert.MatchesAny(whitelist));
                }
            }

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        Organisation = "Organisation"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU0",
                            "OU1",
                            "OU2"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsTrue(baseCert.MatchesAny(whitelist));
                }
            }

            #endregion

            #region Failues

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string> { "OU0", "OU1", "OU2" },
                        Organisation = "Organisation"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU3",
                            "OU4",
                            "OU5"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsFalse(baseCert.MatchesAny(whitelist));
                }
            }

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        CommonName = "DifferentCommonName",
                        OrganisationalUnits = new List<string> { "OU0", "OU1", "OU2" },
                        Organisation = "Organisation"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU0",
                            "OU1",
                            "OU2"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsFalse(baseCert.MatchesAny(whitelist));
                }
            }

            {
                List<CertificateAttributes> whitelist = new List<CertificateAttributes>
                {
                    new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string> { "OU0", "OU1", "OU2" },
                        Organisation = "DifferentOrgnaisation"
                    }
                };

                {
                    var baseCert = new CertificateAttributes
                    {
                        CommonName = "CommonName",
                        OrganisationalUnits = new List<string>
                        {
                            "OU0",
                            "OU1",
                            "OU2"
                        },
                        Organisation = "Organisation"
                    };

                    Assert.IsFalse(baseCert.MatchesAny(whitelist));
                }
            }

            #endregion
        }
    }
}
