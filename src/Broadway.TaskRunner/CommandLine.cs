namespace NuClear.Broadway.TaskRunner
{
    public struct CommandLine
    {
        public const char ArgumentKeySeparator = '=';
        public const char ArgumentValueSeparator = ',';
        public const string HelpOptionTemplate = "-h|--help";

        public struct Commands
        {
            public const string Import = "import";
        }

        public struct CommandTypes
        {
            public const string FlowCardsForERM = "flow-cardsforerm";
            public const string FlowKaleidoscope = "flow-kaleidoscope";
            public const string FlowGeoClassifier = "flow-geoclassifier";
        }

        public struct Arguments
        {
        }
    }
}