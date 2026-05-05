using Rethink.Services.Common.Models.Clients;
using System;
using Xunit;

namespace BillingService.XUnit.Tests.Common.Models
{
    public class RethinkClientDemographicsTests
    {
        #region FullName Tests

        [Fact]
        public void FullName_ShouldConcatenateFirstAndLastName()
        {
            // Arrange
            var client = new RethinkClientDemographics
            {
                FirstName = "Ankit",
                LastName = "Pal"
            };

            // Act
            var result = client.FullName;

            // Assert
            Assert.Equal("Ankit Pal", result);
        }

        #endregion

        #region CalculateAge Tests

        [Fact]
        public void CalculateAge_ShouldReturnCorrectAge_WhenBirthdayAlreadyPassedThisYear()
        {
            // Arrange
            var dob = DateTime.Today.AddYears(-10).AddMonths(-2).AddDays(-5);

            // Act
            var result = RethinkClientDemographics.CalculateAge(dob);

            // Compute expected manually (same logic)
            var expected = CalculateExpectedAge(dob);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculateAge_ShouldReturnCorrectAge_WhenBirthdayNotYetOccurredThisYear()
        {
            // Arrange
            var dob = DateTime.Today.AddYears(-10).AddMonths(1);

            // Act
            var result = RethinkClientDemographics.CalculateAge(dob);

            var expected = CalculateExpectedAge(dob);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Age_Property_ShouldReturnSameAsCalculateAge()
        {
            // Arrange
            var dob = new DateTime(2015, 5, 10);

            var client = new RethinkClientDemographics
            {
                DOB = dob
            };

            // Act
            var result = client.Age;

            // Assert
            Assert.Equal(RethinkClientDemographics.CalculateAge(dob), result);
        }

        #endregion

        #region Helper Method

        private string CalculateExpectedAge(DateTime dob)
        {
            DateTime today = DateTime.Today;

            int months = today.Month - dob.Month;
            int years = today.Year - dob.Year;

            if (today.Day < dob.Day)
            {
                months--;
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            int days = (today - dob.AddMonths(years * 12 + months)).Days;

            return $"{years}y {months}m {days}d";
        }

        #endregion
    }
}
