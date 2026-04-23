namespace POS_system_cs.Application.Services;

public interface IAppLogger
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}
