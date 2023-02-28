using System;
using System.Web;
using Cloud5mins.domain;
using NLog;

public enum LoggerType
{
    UrlShortener,
    UrlPurger
}

/// <summary>
/// A wrapper of NLog
/// </summary>
public class NLogWrapper
{
    private Logger logger;
    private string serviceName;
    private AdminApiSettings shortenerSettings;

    public NLogWrapper(LoggerType loggerType, AdminApiSettings settings)
    {
        initializeLogger(loggerType, settings);
    }

    public void Log(LogLevel logLevel, String message, string arg1 = null, string arg2 = null, string arg3 = null)
    {
        NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("NLog.config");
        var configuration = LogManager.Configuration;
        configuration.FindTargetByName<NLog.Target.Datadog.DataDogTarget>("dataDog").ApiKey = shortenerSettings.dataDogKey;
        LogManager.Configuration = configuration;

        var tags = buildTags();
        logger = logger.WithProperty("service", serviceName).WithProperty("env", shortenerSettings.env).WithProperty("version", shortenerSettings.version).WithProperty("ddtags", tags);

        getCallerName();

        logger.Log(logLevel, message, arg1, arg2);
    }

    private void initializeLogger(LoggerType loggerType, AdminApiSettings settings)
    {
        shortenerSettings = settings;

        string loggerName = String.Empty;

        switch (loggerType)
        {
            case LoggerType.UrlShortener:
                loggerName = "STORIS.UrlShortener";
                serviceName = "UrlShortener";
                break;
            case LoggerType.UrlPurger:
                loggerName = "STORIS.UrlPurger";
                serviceName = "UrlPurger";
                break;
            default:
                break;
        }

        logger = LogManager.GetLogger(loggerName);
    }

    private string buildTags()
    {
        return String.Format("env:{0},service:{1},assemblyversion:{2}", shortenerSettings.env, serviceName, shortenerSettings.version);
    }

    private void getCallerName()
    {
        var methodInfo = new System.Diagnostics.StackTrace().GetFrame(2).GetMethod();
        var callerName = methodInfo.ReflectedType.FullName + '.' + methodInfo.Name;
        logger = logger.WithProperty("method", callerName);
    }

}