using Businesstron.Domain.Services;
using NUnit.Framework;
using Shouldly;

namespace Businesstron.Domain.UnitTests.Services;

public class SuitabilityEvaluatorTests
{
    private static readonly string[] Keywords = ["gov", "legal", "council"];

    [Test]
    public void Clean_name_is_suitable()
    {
        var result = SuitabilityEvaluator.Evaluate("Acme Coffee Roasters", Keywords);

        result.IsSuitable.ShouldBeTrue();
        result.MatchedKeyword.ShouldBeNull();
    }

    [TestCase("Smith Legal Services", "legal")]
    [TestCase("Department of Something GOV", "gov")]
    [TestCase("Shire Council Works", "council")]
    public void Name_containing_keyword_is_unsuitable(string businessName, string expectedKeyword)
    {
        var result = SuitabilityEvaluator.Evaluate(businessName, Keywords);

        result.IsSuitable.ShouldBeFalse();
        result.MatchedKeyword.ShouldBe(expectedKeyword);
    }

    [Test]
    public void Matching_is_case_insensitive()
    {
        SuitabilityEvaluator.Evaluate("GOVERNANCE PARTNERS", Keywords).IsSuitable.ShouldBeFalse();
    }

    [Test]
    public void Empty_or_null_name_is_treated_as_suitable()
    {
        SuitabilityEvaluator.Evaluate(null, Keywords).IsSuitable.ShouldBeTrue();
        SuitabilityEvaluator.Evaluate("   ", Keywords).IsSuitable.ShouldBeTrue();
    }

    [Test]
    public void Blank_keywords_are_ignored()
    {
        SuitabilityEvaluator.Evaluate("Acme Pty Ltd", ["", "   "]).IsSuitable.ShouldBeTrue();
    }
}
