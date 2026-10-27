using ChangaQuest.BusinessLogic.Changas;

namespace ChangaQuest.UnitTests.Changas;

public sealed class ChangaRegistrationValidatorTests
{
    [Fact]
    public void Missing_required_fields_are_reported_by_field()
    {
        var result = ChangaRegistrationValidator.Validate(null, null, null, null, null, null, false);

        Assert.False(result.IsValid);
        Assert.Contains("description", result.Errors.Keys);
        Assert.Contains("reward", result.Errors.Keys);
        Assert.Contains("zone", result.Errors.Keys);
        Assert.Contains("exactAddress", result.Errors.Keys);
        Assert.Contains("materialsNeeded", result.Errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000001)]
    public void Reward_outside_the_ars_limits_is_rejected(int reward)
    {
        var result = ChangaRegistrationValidator.Validate(
            "Descripción válida para una changa.", reward, "Barrio centro", "Dirección ficticia", false, null, false);

        Assert.Contains("reward", result.Errors.Keys);
    }

    [Theory]
    [InlineData("Corta")]
    public void Description_outside_its_limits_is_rejected(string description)
    {
        var result = ChangaRegistrationValidator.Validate(description, 1000, "Barrio centro", "Dirección ficticia", false, null, false);

        Assert.Contains("description", result.Errors.Keys);
    }

    [Fact]
    public void Description_over_the_maximum_is_rejected()
    {
        var result = ChangaRegistrationValidator.Validate(
            new string('x', 1001), 1000, "Barrio centro", "Direcci?n ficticia", false, null, false);

        Assert.Contains("description", result.Errors.Keys);
    }

    [Theory]
    [InlineData("ab", "Calle privada 123")]
    [InlineData("Barrio centro", "1234")]
    [InlineData("Barrio centro", "Direcci?n ficticia que supera el l?mite de caracteres permitido por la pol?tica de publicaci?n cuando se le agregan suficientes caracteres adicionales para llegar a m?s de doscientos caracteres y que por lo tanto debe ser rechazada por el validador de direcci?n exacta privada antes de persistir la changa, manteniendo en todo momento la privacidad requerida para la informaci?n sensible del domicilio exacto de quien solicita el trabajo en el barrio local.")]
    public void Zone_and_address_minimums_and_address_maximum_are_enforced(string zone, string exactAddress)
    {
        var result = ChangaRegistrationValidator.Validate(
            "Descripci?n v?lida para una changa.", 1000, zone, exactAddress, false, null, false);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Materials_provider_is_required_when_materials_are_needed()
    {
        var result = ChangaRegistrationValidator.Validate(
            "Descripción válida para una changa.", 1000, "Barrio centro", "Dirección ficticia", true, null, false);

        Assert.Contains("materialsProvider", result.Errors.Keys);
    }

    [Fact]
    public void Materials_provider_must_be_omitted_when_materials_are_not_needed()
    {
        var result = ChangaRegistrationValidator.Validate(
            "Descripción válida para una changa.", 1000, "Barrio centro", "Dirección ficticia", false, "worker", true);

        Assert.Contains("materialsProvider", result.Errors.Keys);
    }

    [Fact]
    public void A_valid_publication_passes_validation()
    {
        var result = ChangaRegistrationValidator.Validate(
            "Descripción válida para una changa.", 1000, "Barrio centro", "Dirección ficticia", true, "requester", true);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Public_projection_does_not_expose_the_exact_address()
    {
        Assert.DoesNotContain(typeof(ChangaPublicView).GetProperties(), property => property.Name == "ExactAddress");
    }
}
