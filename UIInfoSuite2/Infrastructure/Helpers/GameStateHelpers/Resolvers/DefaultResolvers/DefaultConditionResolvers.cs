using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

internal static partial class DefaultConditionResolvers
{
  public static ConditionResolver UnsupportedConditionResolver(string conditionKey)
  {
    return new ConditionResolver(
      conditionKey,
      (joinedQueryString, _) => I18n.GSQ_Requirements_Unsupported().Format(joinedQueryString, conditionKey),
      true
    );
  }
}
