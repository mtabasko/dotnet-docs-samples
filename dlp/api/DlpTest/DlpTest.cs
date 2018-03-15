// Copyright 2018 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xunit;

namespace GoogleCloudSamples
{
    public class DlpTest
    {
        private readonly string _projectId;
        const string phone = "(223) 456-7890";
        const string email = "gary@somedomain.org";
        const string cc = "4000-3000-2000-1000";
        const string inspectFilePath = "test.txt";
        const string inspectStringValue = "hi my phone number is (223) 456-7890 and my email address is gary@somedomain.org. " +
            "You'll find my credit card number is 4000-3000-2000-1000!";

        readonly CommandLineRunner _dlp = new CommandLineRunner()
        {
            VoidMain = Dlp.Main,
            Command = "Dlp"
        };

        protected ConsoleOutput Run(params string[] args)
        {
            string cmd = args[0].ToLower();
            return _dlp.Run(args);
        }

        public DlpTest()
        {
            _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
        }

        private void AssertPhoneEmailCC(ConsoleOutput output, bool checkPhone, bool checkEmail, bool checkCc)
        {
            var results = 0;
            if (checkPhone) results++;
            if (checkEmail) results++;
            if (checkCc) results++;
            Assert.Contains($"Found {results} results", output.Stdout);
            if (checkPhone) Assert.Contains(phone, output.Stdout);
            if (checkEmail) Assert.Contains(email, output.Stdout);
            if (checkCc) Assert.Contains(cc, output.Stdout);
        }

        /// <summary>
        /// With default params, Dlp will find matches of all info types and likelihoods, including quote info.
        /// </summary>
        [Theory]
        [InlineData("inspectString", inspectStringValue)]
        [InlineData("inspectFile", inspectFilePath)]
        public void TestInspectStringFilterInfoTypes(string verb, string value)
        {
            AssertPhoneEmailCC(Run(verb, _projectId, value), true, true, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "PHONE_NUMBER,EMAIL_ADDRESS,CREDIT_CARD_NUMBER"), true, true, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "PHONE_NUMBER,EMAIL_ADDRESS"), true, true, false);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "PHONE_NUMBER,CREDIT_CARD_NUMBER"), true, false, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "EMAIL_ADDRESS,CREDIT_CARD_NUMBER"), false, true, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "PHONE_NUMBER"), true, false, false);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "EMAIL_ADDRESS"), false, true, false);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-i", "CREDIT_CARD_NUMBER"), false, false, true);
        }

        /// <summary>
        /// Dlp evaluates this content string as having 
        /// PHONE_NUMBER - VERY_LIKELY
        /// EMAIL_ADDRESS - LIKELY
        /// CREDIT_CARD_NUMBER - VERY_LIKELY
        /// </summary>
        [Theory]
        [InlineData("inspectString", inspectStringValue)]
        [InlineData("inspectFile", inspectFilePath)]
        public void TestInspectStringFilterLikelihood(string verb, string value)
        {
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-l", "4"), true, true, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-l", "5"), true, false, true);
        }

        /// <summary>
        /// Dlp finds the matches in the order phone -> email -> credit card
        /// </summary>
        [Theory]
        [InlineData("inspectString", inspectStringValue)]
        [InlineData("inspectFile", inspectFilePath)]
        public void TestInspectStringFilterMaxResults(string verb, string value)
        {
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-m", "0"), true, true, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-m", "3"), true, true, true);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-m", "2"), true, true, false);
            AssertPhoneEmailCC(Run(verb, _projectId, value, "-m", "1"), true, false, false);
        }
    }

    //public class QuickStartTests
    //{
    //    readonly CommandLineRunner _quickStart = new CommandLineRunner()
    //    {
    //        VoidMain = QuickStart.Main,
    //        Command = "dotnet run"
    //    };

    //    [Fact]
    //    public void TestRun()
    //    {
    //        var output = _quickStart.Run();
    //        Assert.Equal(0, output.ExitCode);
    //    }
    //}
}
