namespace Benday.CosmosDb.SampleApp.Tests;

public static class TestUtilities
{
    private const string DEPENDENCY_ERROR_START = "Unable to resolve service for type '";
    private const string DEPENDENCY_ERROR_END = "' while attempting to activate '";

    public static void CheckForDependencyError(string content)
    {
        // Unable to resolve service for type 'Benday.DemoApp.Api.IDecisionService' while attempting to activate 'Benday.DemoApp.Web.Controllers.DecisionController'

        if (content.Contains(DEPENDENCY_ERROR_START) == true)
        {
            Assert.Fail(ParseDependencyError(content));
        }
    }

    public static string ParseDependencyError(string content)
    {
        if (content.Contains(DEPENDENCY_ERROR_START) == true)
        {
            var indexOfStart = content.IndexOf(DEPENDENCY_ERROR_START);
            var indexOfEnd = content.IndexOf(DEPENDENCY_ERROR_END);

            if (indexOfStart == -1 || indexOfEnd == -1)
            {
                return $"Got a dependency error but was unable to parse it. {content}";
            }
            else
            {
                var lengthOfFirstPartOfError = indexOfEnd - indexOfStart;

                var interfaceType = content.Substring(indexOfStart, lengthOfFirstPartOfError);

                interfaceType = interfaceType.Replace(DEPENDENCY_ERROR_START, "");

                var concreteType = content.Substring(indexOfEnd + DEPENDENCY_ERROR_END.Length);

                // if there is a single quote, remove everything after the single quote
                var indexOfSingleQuote = concreteType.IndexOf("'");

                if (indexOfSingleQuote > 0)
                {
                    concreteType = concreteType.Substring(0, indexOfSingleQuote);
                }

                var returnValue = $"Dependency error. Interface '{interfaceType}' is not resolved to concrete type '{concreteType}'.";

                return returnValue;
            }
        }
        else
        {
            return content;
        }
    }
}