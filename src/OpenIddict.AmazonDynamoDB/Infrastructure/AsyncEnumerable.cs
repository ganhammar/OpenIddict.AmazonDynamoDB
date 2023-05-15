namespace OpenIddict.AmazonDynamoDB;

public static class AsyncEnumerable
{
  public static IAsyncEnumerable<T> Empty<T>()
  {
    return new EmptyAsyncEnumerable<T>();
  }
}
