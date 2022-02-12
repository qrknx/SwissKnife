namespace SwissKnife.Serverless.Shared;

public interface IFormFragment<out T>
{
    public T Data { get; }
}
