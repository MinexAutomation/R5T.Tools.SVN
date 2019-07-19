using System;

using R5T.Neapolis;


namespace R5T.Tools.SVN
{
    public static class IArgumentsBuilderExtensions
    {
        public static IArgumentsBuilder AddVerbose(this IArgumentsBuilder argumentsBuilder)
        {
            argumentsBuilder.AddFlagShort("v");

            return argumentsBuilder;
        }

        public static IArgumentsBuilder SetDepth(this IArgumentsBuilder argumentsBuilder, string depth)
        {
            argumentsBuilder
                .AddNameValue(nameValue =>
                {
                    nameValue
                        .SetNameFull("depth")
                        .SetValue(depth)
                        ;
                });

            return argumentsBuilder;
        }

        public static IArgumentsBuilder ForInstanceOnly(this IArgumentsBuilder argumentsBuilder)
        {
            argumentsBuilder.SetDepth("empty");

            return argumentsBuilder;
        }

        public static IArgumentsBuilder AddXml(this IArgumentsBuilder argumentsBuilder)
        {
            argumentsBuilder.AddFlagFull("xml");

            return argumentsBuilder;
        }
    }
}
