// Copyright (c) 2018 Google LLC.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.

using System;
using CommandLine;
using Google.Cloud.Dlp.V2;

namespace GoogleCloudSamples
{
    public partial class Dlp
    {
        [Verb("listInfoTypes", HelpText = "List the types of sensitive information the DLP API supports.")]
        class ListInfoTypesOptions
        {
            [Value(0, HelpText = "The filter to use.", Required = true)]
            public string Filter { get; set; }

            [Value(1, HelpText = "The BCP-47 language code to use.", Default = "en-US")]
            public string LanguageCode { get; set; }
        }

        static object ListInfoTypes(ListInfoTypesOptions opts)
        {
            DlpServiceClient dlp = DlpServiceClient.Create();

            var response = dlp.ListInfoTypes(new ListInfoTypesRequest
            {
                LanguageCode = opts.LanguageCode,
                Filter = opts.Filter
            });

            Console.WriteLine("Info types:");
            foreach (var infoType in response.InfoTypes)
            {
                Console.WriteLine($"  {infoType.Name} ({infoType.DisplayName})");
            }

            return 0;
        }
    }
}
