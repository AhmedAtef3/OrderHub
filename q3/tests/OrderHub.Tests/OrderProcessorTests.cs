using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Services;
using OrderHub.Application.UseCases;
using OrderHub.Core.Abstractions;
using OrderHub.Core.Models;
using Xunit;

namespace OrderHub.Tests;

public sealed class PricingCalculatorTests
{
    [Fact]
    public void GoldTier_Applies_15_Percent_Discount()
    {
        var result = PricingCalculator.CalculateUnitPrice(100m, PricingTier.Gold, null);
        Assert.Equal(85.00m, result);
    }

    [Fact]
    public void SilverTier_Applies_8_Percent_Discount()
    {
        var result = PricingCalculator.CalculateUnitPrice(100m, PricingTier.Silver, null);
        Assert.Equal(92.00m, result);
    }

    [Fact]
    public void StandardTier_Applies_No_Discount()
    {
        var result = PricingCalculator.CalculateUnitPrice(100m, PricingTier.Standard, null);
        Assert.Equal(100.00m, result);
    }

    [Theory]
    [InlineData("AB", 4.50)]
    [InlineData("ABC", 4.50)]
    public void Embroidery_Short_Adds_4_50(string text, decimal expected)
    {
        var result = PricingCalculator.CalculateUnitPrice(0m, PricingTier.Standard, text);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ABCD", 8.00)]
    [InlineData("SMITH", 8.00)]
    public void Embroidery_Long_Adds_8_00(string text, decimal expected)
    {
        var result = PricingCalculator.CalculateUnitPrice(0m, PricingTier.Standard, text);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GoldTier_Plus_LongEmbroidery_CombinesCorrectly()
    {
        var result = PricingCalculator.CalculateUnitPrice(100m, PricingTier.Gold, "SMITH");
        Assert.Equal(93.00m, result);
    }

    public sealed class ProcessOrderUseCaseTests
    {
        private const int SchoolId = 42;
        private const string ParentEmail = "parent@example.com";
        private const string Sku = "SHIRT-L";
        private const string OrderRef = "PAY-001";

        // All dependencies are mocked — Infrastructure never loaded
        private readonly ISchoolRepository _schools = Substitute.For<ISchoolRepository>();
        private readonly IProductRepository _products = Substitute.For<IProductRepository>();
        private readonly IStockRepository _stock = Substitute.For<IStockRepository>();
        private readonly IPaymentService _payments = Substitute.For<IPaymentService>();
        private readonly INotificationService _notifications = Substitute.For<INotificationService>();

        private ProcessOrderUseCase BuildUseCase()
        {
            return new(_schools, _products, _stock, _payments, _notifications, NullLogger<ProcessOrderUseCase>.Instance);
        }

        private void SetupHappyPath(PricingTier tier = PricingTier.Standard, decimal basePrice = 20m, int availableStock = 10)
        {
            _schools.GetPricingTierAsync(SchoolId).Returns(tier);
            _products.GetBasePriceAsync(Sku).Returns(basePrice);
            _stock.GetAvailableStockAsync(Sku).Returns(availableStock);
            _payments.CreatePaymentIntentAsync(Arg.Any<decimal>(), ParentEmail, Arg.Any<string>()).Returns(OrderRef);
        }

        [Fact]
        public async Task Execute_ReturnsFailure_WhenStockInsufficient()
        {
            _schools.GetPricingTierAsync(SchoolId).Returns(PricingTier.Standard);
            _products.GetBasePriceAsync(Sku).Returns(20m);
            _stock.GetAvailableStockAsync(Sku).Returns(2);

            var result = await BuildUseCase().ExecuteAsync(SchoolId, [new(Sku, Quantity: 3)], ParentEmail);

            var failure = Assert.IsType<OrderResult.Failure>(result);
            Assert.Contains(Sku, failure.Reason);

            await _payments.DidNotReceiveWithAnyArgs().CreatePaymentIntentAsync(default, default!, default!);
        }

        [Fact]
        public async Task Execute_ReturnsFailure_WhenSchoolNotFound()
        {
            _schools.GetPricingTierAsync(SchoolId).Returns((PricingTier?)null);

            var result = await BuildUseCase().ExecuteAsync(SchoolId, [new(Sku, 1)], ParentEmail);

            Assert.IsType<OrderResult.Failure>(result);
        }

        [Fact]
        public async Task Execute_AppliesGoldDiscount_ToSubtotal()
        {
            SetupHappyPath(PricingTier.Gold, basePrice: 100m);

            _payments.CreatePaymentIntentAsync(170m, ParentEmail, Arg.Any<string>()).Returns(OrderRef);

            var result = await BuildUseCase().ExecuteAsync(SchoolId, [new(Sku, Quantity: 2)], ParentEmail);

            var success = Assert.IsType<OrderResult.Success>(result);

            Assert.Equal(170m, success.Total);
        }

        [Fact]
        public async Task Execute_ReturnsFailure_WhenPaymentProviderFails()
        {
            SetupHappyPath();

            _payments.CreatePaymentIntentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new PaymentException("gateway timeout"));

            var result = await BuildUseCase().ExecuteAsync(SchoolId, [new(Sku, 1)], ParentEmail);

            Assert.IsType<OrderResult.Failure>(result);
        }

        [Fact]
        public async Task Execute_Succeeds_EvenWhen_NotificationFails()
        {
            SetupHappyPath();
            _notifications.SendOrderConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>())
                .ThrowsAsync(new Exception("SMTP down"));

            var result = await BuildUseCase()
                .ExecuteAsync(SchoolId, [new(Sku, 1)], ParentEmail);

            Assert.IsType<OrderResult.Success>(result);
        }

        [Fact]
        public async Task Execute_Returns_OrderReference_OnHappyPath()
        {
            SetupHappyPath(basePrice: 20m);

            var result = await BuildUseCase().ExecuteAsync(SchoolId, [new(Sku, 1)], ParentEmail);

            var success = Assert.IsType<OrderResult.Success>(result);
            Assert.Equal(OrderRef, success.OrderReference);
            Assert.Equal(20m, success.Total);
        }
    }
}
