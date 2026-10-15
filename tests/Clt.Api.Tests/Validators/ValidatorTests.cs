using Clt.Api.Application.Addresses.Commands;
using Clt.Api.Application.Currencies.Commands;
using Clt.Api.Application.CurrencyConversion;
using Clt.Api.Application.Users.Commands;
using FluentValidation.TestHelper;

namespace Clt.Api.Tests.Validators;

public class ValidatorTests
{
    [Fact]
    public void User_without_password_is_valid_but_an_explicit_empty_password_is_not()
    {
        var validator = new CreateUserValidator();
        validator.TestValidate(new CreateUserCommand("Juan", "juan@test.com")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new CreateUserCommand("Juan", "juan@test.com", " "))
            .ShouldHaveValidationErrorFor(request => request.Password);
    }

    [Theory]
    [InlineData("", "valid@test.com", "Name")]
    [InlineData(" ", "valid@test.com", "Name")]
    [InlineData("Juan", "", "Email")]
    [InlineData("Juan", "invalid", "Email")]
    public void Create_and_update_user_validate_required_fields(string name, string email, string property)
    {
        new CreateUserValidator().TestValidate(new CreateUserCommand(name, email)).ShouldHaveValidationErrorFor(property);
        new UpdateUserValidator().TestValidate(new UpdateUserCommand(name, email, true)).ShouldHaveValidationErrorFor(property);
    }

    [Theory]
    [InlineData("", "City", "Country", "Street")]
    [InlineData("Street", "", "Country", "City")]
    [InlineData("Street", "City", "", "Country")]
    public void Create_and_update_address_validate_required_fields(string street, string city, string country, string property)
    {
        new CreateAddressValidator().TestValidate(new CreateAddressCommand(street, city, country))
            .ShouldHaveValidationErrorFor(property);
        new UpdateAddressValidator().TestValidate(new UpdateAddressCommand(street, city, country))
            .ShouldHaveValidationErrorFor(property);
    }

    [Fact]
    public void Zip_code_is_optional_on_create_and_update()
    {
        new CreateAddressValidator().TestValidate(new CreateAddressCommand("Street", "City", "Country"))
            .ShouldNotHaveAnyValidationErrors();
        new UpdateAddressValidator().TestValidate(new UpdateAddressCommand("Street", "City", "Country"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Currency_requires_code_name_and_positive_rate()
    {
        var validator = new CreateCurrencyValidator();
        var result = validator.TestValidate(new CreateCurrencyCommand("", "", 0));
        result.ShouldHaveValidationErrorFor(request => request.Code);
        result.ShouldHaveValidationErrorFor(request => request.Name);
        result.ShouldHaveValidationErrorFor(request => request.RateToBase);
        validator.TestValidate(new CreateCurrencyCommand("PYG", "Guaraní", 1)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Conversion_requires_both_codes_and_positive_amount()
    {
        var validator = new ConvertCurrencyValidator();
        var result = validator.TestValidate(new ConvertCurrencyQuery("", "", -1));
        result.ShouldHaveValidationErrorFor(request => request.FromCurrencyCode);
        result.ShouldHaveValidationErrorFor(request => request.ToCurrencyCode);
        result.ShouldHaveValidationErrorFor(request => request.Amount);
        validator.TestValidate(new ConvertCurrencyQuery("USD", "PYG", 100)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Bulk_validates_container_and_leaves_item_validation_to_each_operation()
    {
        var validator = new BulkCreateUsersValidator();
        validator.TestValidate(new BulkCreateUsersCommand(null)).ShouldHaveValidationErrorFor(request => request.Users);
        validator.TestValidate(new BulkCreateUsersCommand([])).ShouldHaveValidationErrorFor(request => request.Users);
        validator.TestValidate(new BulkCreateUsersCommand([new CreateUserCommand("", "invalid")]))
            .ShouldNotHaveAnyValidationErrors();
    }
}
