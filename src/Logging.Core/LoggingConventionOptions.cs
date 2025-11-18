using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Errors.Logging;

public sealed class LoggingConventionOptions
{
    public bool ClearProviders { get; set; } = true;

    public bool UseConfiguration { get; set; } = true;

    public string ConfigurationSectionName { get; set; } = "Logging";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public IList<Action<ILoggingBuilder>> Providers { get; } = new List<Action<ILoggingBuilder>>
    {
        builder => builder.AddConsole(),
        builder => builder.AddDebug()
    };

    public Action<ILoggingBuilder>? ConfigureBuilder { get; set; }
}
