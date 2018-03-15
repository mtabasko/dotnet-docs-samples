/**
 * Copyright 2017, Google, Inc.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using CommandLine;
using Google.Cloud.Dlp.V2;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Google.Cloud.Dlp.V2.InspectConfig.Types;
using static Google.Cloud.Dlp.V2.JobTrigger.Types;
using static Google.Cloud.Dlp.V2.CloudStorageOptions.Types;

namespace GoogleCloudSamples
{
    public partial class Dlp
    {

        static void Quickstart()
        {
            // [START dlp_quickstart]
            DlpServiceClient dlp = DlpServiceClient.Create();

            // The value to inspect
            string String = "Robert Frost";

            // The project ID to run the API call under
            string ProjectId = "YOUR PROJECT ID";

            // The minimum likelihood required before returning a match
            Likelihood MinLikelihood = Likelihood.Unspecified;

            // The maximum number of findings to report (0 = server maximum)
            int MaxFindings = 0;

            // Whether to include the matching string
            bool IncludeQuote = true;

            InspectConfig config = new InspectConfig
            {
                MinLikelihood = MinLikelihood,
                IncludeQuote = IncludeQuote,
                Limits = new FindingLimits
                {
                    MaxFindingsPerRequest = MaxFindings
                }
            };

            // Configure the infoTypes of information to match
            config.InfoTypes.Add(new InfoType { Name = "PERSON_NAME" });
            config.InfoTypes.Add(new InfoType { Name = "US_STATE" });

            var response = dlp.InspectContent(new InspectContentRequest
            {
                Parent = $"projects/{ProjectId}",
                InspectConfig = config,
                Item = new ContentItem { Value = String }
            });

            var Findings = response.Result.Findings;
            if (Findings.Any()) {
                Console.WriteLine("Findings: ");
                foreach (Finding finding in Findings)
                {
                    if (!string.IsNullOrEmpty(finding.Quote)) {
                        Console.WriteLine($"  Quote: {finding.Quote}");
                    }
                    Console.WriteLine($"\tInfo type: { finding.InfoType.Name}");
                    Console.WriteLine($"\tLikelihood: { finding.Likelihood}");
                }
            }
            // [END dlp_quickstart]
        }
    }
}