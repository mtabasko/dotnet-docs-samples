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

using CommandLine;
using Google.Cloud.Dlp.V2;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleCloudSamples
{
    abstract class InspectLocalOptions
    {
        [Value(0, HelpText = "The project ID to run the API call under.", Required = true)]
        public string ProjectId { get; set; }
        [Option('i', "info-types", HelpText = "Comma-separated infoTypes of information to match.",
            Default = "PHONE_NUMBER,EMAIL_ADDRESS,CREDIT_CARD_NUMBER")]
        public string InfoTypes { get; set; }
        [Option('l', "minimum-likelihood",
            HelpText = "The minimum likelihood required before returning a match (0-5).", Default = 0)]
        public int MinLikelihood { get; set; }
        [Option('m', "max-findings",
            HelpText = "The maximum number of findings to report per request (0 = server maximum).", Default = 0)]
        public int MaxFindings { get; set; }
        [Option('n', "no-includeQuotes", HelpText = "Do not include matching quotes.")]
        public bool NoIncludeQuote { get; set; }
    }

    [Verb("inspectString", HelpText = "Inspects a content string.")]
    class InspectStringOptions : InspectLocalOptions
    {
        [Value(1, HelpText = "The item to inspect.", Required = true)]
        public string Value { get; set; }
    }

    [Verb("inspectFile", HelpText = "Inspects a content file.")]
    class InspectFileOptions : InspectLocalOptions
    {
        [Value(1, HelpText = "The path to the local file to inspect. Can be a text, JPG, or PNG.", Required = true)]
        public string File { get; set; }
    }

    public partial class Dlp
    {
        static IEnumerable<InfoType> ParseInfoTypes(string infoTypesStr)
        {
            return infoTypesStr.Split(',').Select(str =>
            {
                try
                {
                    return InfoType.Parser.ParseJson($"{{\"name\": \"{str}\"}}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse infoType {str}: {e}");
                    return null;
                }
            }).Where(it => it != null);
        }

        static object InspectString(InspectStringOptions opts)
        {
            return InspectLocal(opts, new InspectContentRequest
            {
                Parent = $"projects/{opts.ProjectId}",
                Item = new ContentItem
                {
                    Value = opts.Value
                }
            });
        }

        static readonly Dictionary<string, ByteContentItem.Types.BytesType> fileTypes =
            new Dictionary<string, ByteContentItem.Types.BytesType>()
        {
            {"bmp", ByteContentItem.Types.BytesType.ImageBmp},
            {"jpg", ByteContentItem.Types.BytesType.ImageJpeg},
            {"jpeg", ByteContentItem.Types.BytesType.ImageJpeg},
            {"png", ByteContentItem.Types.BytesType.ImagePng},
            {"svg", ByteContentItem.Types.BytesType.ImageSvg},
            {"txt", ByteContentItem.Types.BytesType.TextUtf8}
        };

        static object InspectFile(InspectFileOptions opts)
        {
            var fileStream = new FileStream(opts.File, FileMode.Open);
            try
            {
                return InspectLocal(opts, new InspectContentRequest
                {
                    Parent = $"projects/{opts.ProjectId}",
                    Item = new ContentItem
                    {
                        ByteItem = new ByteContentItem
                        {
                            Data = ByteString.FromStream(fileStream),
                            Type = fileTypes.GetValueOrDefault(new FileInfo(opts.File).Extension.ToLower(),
                                    ByteContentItem.Types.BytesType.Unspecified)
                        }
                    }
                });
            } finally
            {
                fileStream.Close();
            }
        }

        private static object InspectLocal(InspectLocalOptions opts,
            InspectContentRequest request)
        {
            var inspectConfig = new InspectConfig
            {
                MinLikelihood = (Likelihood)opts.MinLikelihood,
                Limits = new InspectConfig.Types.FindingLimits
                {
                    MaxFindingsPerRequest = opts.MaxFindings
                },
                IncludeQuote = !opts.NoIncludeQuote
            };
            inspectConfig.InfoTypes.AddRange(ParseInfoTypes(opts.InfoTypes));
            request.InspectConfig = inspectConfig;
            DlpServiceClient dlp = DlpServiceClient.Create();
            InspectContentResponse response = dlp.InspectContent(request);
            var count = 0;
            var findingsStr = "";
            foreach (var finding in response.Result.Findings)
            {
                findingsStr += $"\nFinding {count++}: \n\t{finding}";
            }
            var wereOrNotTruncated = "were" + (response.Result.FindingsTruncated ? "" : " not") + " truncated";
            Console.WriteLine($"Found {count} results, and results {wereOrNotTruncated}: {findingsStr}");
            return 0;
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<
                InspectStringOptions,
                InspectFileOptions,
                CreateJobTriggerOptions,
                DeleteJobTriggerOptions,
                ListJobTriggersOptions,
                ListJobsOptions,
                DeleteJobOptions,
                DeidentifyMaskingOptions,
                DeidentifyDateShiftOptions,
                ListInfoTypesOptions,
                NumericalStatsOptions,
                CategoricalStatsOptions,
                KAnonymityOptions,
                LDiversityOptions
            >(args)
                .MapResult(
                (DeidentifyMaskingOptions opts) => DeidentifyMasking(opts),
                (ListJobsOptions opts) => ListJobs(opts),
                (DeleteJobOptions opts) => DeleteJob(opts),
                (CreateJobTriggerOptions opts) => CreateJobTrigger(opts),
                (ListJobTriggersOptions opts) => ListJobTriggers(opts),
                (DeleteJobTriggerOptions opts) => DeleteJobTrigger(opts),
                (InspectStringOptions opts) => InspectString(opts),
                (DeidentifyDateShiftOptions opts) => DeidentifyDateShift(opts),
                (ListInfoTypesOptions opts) => ListInfoTypes(opts),
                (NumericalStatsOptions opts) => NumericalStats(opts),
                (CategoricalStatsOptions opts) => CategoricalStats(opts),
                (KAnonymityOptions opts) => KAnonymity(opts),
                (LDiversityOptions opts) => LDiversity(opts),
                (InspectFileOptions opts) => {
                    Console.WriteLine($"projectID: {opts.ProjectId}\nfile: {opts.File}\ninfoTypes: {opts.InfoTypes}\nlikelihood: {opts.MinLikelihood}\nmaxResults: {opts.MaxFindings}");
                    return InspectFile(opts); },
                errs => 1);
        }
    }
}
