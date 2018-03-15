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
using static Google.Cloud.Dlp.V2.InspectConfig.Types;
using static Google.Cloud.Dlp.V2.JobTrigger.Types;
using static Google.Cloud.Dlp.V2.CloudStorageOptions.Types;

namespace GoogleCloudSamples
{
    [Verb("deidMasking", HelpText = "Create a Data Loss Prevention API job trigger.")]
    class DeidentifyMaskingOptions
    {
        [Value(0, HelpText = "The project ID to run the API call under.", Required = true)]
        public string ProjectId { get; set; }

        [Value(1, HelpText = "The string to deidentify.", Required = true)]
        public string Value { get; set; }

        [Value(2, HelpText = "The character to mask matching sensitive data with.", Required = true)]
        public char MaskingCharacter { get; set; }

        [Value(3, HelpText = "The maximum number of sensitive characters to mask in a match. (0 = mask all)", Default = 0)]
        public int NumberToMask { get; set; }
    }

    [Verb("deidDateShift", HelpText = "Deidentify dates in a CSV file by pseudorandomly shifting them.")]
    class DeidentifyDateShiftOptions
    {
        [Value(0, HelpText = "The project ID to run the API call under.", Required = true)]
        public string ProjectId { get; set; }

        [Value(1, HelpText = "The path to the CSV file to deidentify.", Required = true)]
        public string InputCsvFile { get; set; }

        [Value(2, HelpText = "The path to save the date-shifted CSV file to.", Required = true)]
        public string OutputCsvFile { get; set; }

        [Value(3, HelpText = "The maximum number of days to shift a date backward.", Required = true)]
        public int LowerBoundDays { get; set; }

        [Value(4, HelpText = "The maximum number of days to shift a date forward.", Required = true)]
        public int UpperBoundDays { get; set; }

        [Value(5, HelpText = "The column to determine date shift amount based on.")]
        public string ContextFieldId { get; set; }

        [Value(6, HelpText = "The name of the Cloud KMS key used to encrypt('wrap') the AES-256 key.")]
        public string WrappedKey { get; set; }

        [Value(7, HelpText = "The encrypted('wrapped') AES-256 key to use when shifting dates.")]
        public string KeyName { get; set; }

        [Option('d', "dateFields", Required = true, Min = 1)]
        public IEnumerable<string> DateFields { get; set; }
    }

    public partial class Dlp
    {
        static object DeidentifyMasking(DeidentifyMaskingOptions opts)
        {
            DlpServiceClient dlp = DlpServiceClient.Create();

            CharacterMaskConfig maskConfig = new CharacterMaskConfig
            {
                MaskingCharacter = Char.ToString(opts.MaskingCharacter),
                NumberToMask = opts.NumberToMask
            };

            DeidentifyConfig deidConfig = new DeidentifyConfig
            {
                InfoTypeTransformations = new InfoTypeTransformations
                {
                    Transformations = {
                        new InfoTypeTransformations.Types.InfoTypeTransformation {
                            PrimitiveTransformation = new PrimitiveTransformation {
                                CharacterMaskConfig = maskConfig
                            }
                        }
                    }
                }
            };

            var response = dlp.DeidentifyContent(new DeidentifyContentRequest
            {
                Parent = $"projects/{opts.ProjectId}",
                DeidentifyConfig = deidConfig,
                Item = new ContentItem
                {
                    Value = opts.Value
                }
            });

            Console.WriteLine(response.Item.Value);

            return 0;
        }

        static object DeidentifyDateShift(DeidentifyDateShiftOptions opts)
        {
            DlpServiceClient dlp = DlpServiceClient.Create();

            // Read file
            string[] CsvLines = File.ReadAllLines(opts.InputCsvFile);
            string[] CsvHeaders = CsvLines[0].Split(',');
            string[] CsvRows = CsvLines.Skip(1).ToArray();

            // Convert to protobuf format
            var ProtoHeaders = CsvHeaders.Select(header => new FieldId { Name = header });
            var ProtoRows = CsvRows.Select(CsvRow =>
            {
                var RowValues = CsvRow.Split(',');
                var ProtoValues = RowValues.Select(RowValue =>
                {
                    System.DateTime ParsedDate;
                    if (System.DateTime.TryParse(RowValue, out ParsedDate))
                    {
                        return new Value
                        {
                            DateValue = new Google.Type.Date
                            {
                                Year = ParsedDate.Year,
                                Month = ParsedDate.Month,
                                Day = ParsedDate.Day
                            }
                        };
                    }

                    return new Value
                    {
                        StringValue = RowValue
                    };
                });

                var RowObject = new Table.Types.Row();
                RowObject.Values.Add(ProtoValues);
                return RowObject;
            });

            var DateFields = opts.DateFields.Select(field => new FieldId { Name = field });

            // Construct + execute the request
            DateShiftConfig Config = new DateShiftConfig
            {
                LowerBoundDays = opts.LowerBoundDays,
                UpperBoundDays = opts.UpperBoundDays
            };
            bool hasKeyName = !String.IsNullOrEmpty(opts.KeyName);
            bool hasWrappedKey = !String.IsNullOrEmpty(opts.WrappedKey);
            bool hasContext = !String.IsNullOrEmpty(opts.ContextFieldId);
            if (hasKeyName && hasWrappedKey && hasContext)
            {
                Config.Context = new FieldId { Name = opts.ContextFieldId };
                Config.CryptoKey = new CryptoKey
                {
                    KmsWrapped = new KmsWrappedCryptoKey
                    {
                        WrappedKey = Google.Protobuf.ByteString.FromBase64(opts.WrappedKey),
                        CryptoKeyName = opts.KeyName
                    }
                };
            }
            else if (hasKeyName || hasWrappedKey || hasContext)
            {
                throw new ArgumentException("Must specify ALL or NONE of: {contextFieldId, keyName, wrappedKey}!");
            }

            FieldTransformation Transformation = new FieldTransformation
            {
                PrimitiveTransformation = new PrimitiveTransformation
                {
                    DateShiftConfig = Config
                }
            };
            Transformation.Fields.Add(DateFields);

            DeidentifyConfig deidConfig = new DeidentifyConfig
            {
                RecordTransformations = new RecordTransformations
                {
                    FieldTransformations = { Transformation }
                }
            };

            Table TableItem = new Table();
            TableItem.Headers.Add(ProtoHeaders);
            TableItem.Rows.Add(ProtoRows);

            var response = dlp.DeidentifyContent(new DeidentifyContentRequest
            {
                Parent = $"projects/{opts.ProjectId}",
                DeidentifyConfig = deidConfig,
                Item = new ContentItem
                {
                    Table = TableItem
                }
            });

            // Save the results
            List<String> OutputLines = new List<string>();
            OutputLines.Add(CsvLines[0]);

            OutputLines.AddRange(response.Item.Table.Rows.Select(ProtoRow =>
            {
                var Values = ProtoRow.Values.Select(ProtoValue =>
                {
                    if (ProtoValue.DateValue != null)
                    {
                        var ProtoDate = ProtoValue.DateValue;
                        System.DateTime Date = new System.DateTime(
                            ProtoDate.Year, ProtoDate.Month, ProtoDate.Day);
                        return Date.ToString();
                    }
                    else
                    {
                        return ProtoValue.StringValue;
                    }
                });
                return String.Join(',', Values);
            }));

            File.WriteAllLines(opts.OutputCsvFile, OutputLines);

            return 0;
        }
    }
}