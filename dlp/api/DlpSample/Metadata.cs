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

            var response = dlp.ListInfoTypes(new ListInfoTypesRequest{
                LanguageCode = opts.LanguageCode,
                Filter = opts.Filter
            });

            Console.WriteLine("Info types:");
            foreach (var infoType in response.InfoTypes) {
                Console.WriteLine($"  {infoType.Name} ({infoType.DisplayName})");
            }

            return 0;
        }
    }
}
