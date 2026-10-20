namespace ChangaQuest.BusinessLogic.Accounts;

public sealed record AccountRegistrationValidationResult(
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
