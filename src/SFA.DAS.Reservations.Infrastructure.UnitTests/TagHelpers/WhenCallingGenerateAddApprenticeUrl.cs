﻿using System;
using AutoFixture.NUnit3;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.Reservations.Infrastructure.Configuration;
using SFA.DAS.Reservations.Infrastructure.TagHelpers;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.Reservations.Infrastructure.UnitTests.TagHelpers
{
    [TestFixture]
    public class WhenCallingGenerateAddApprenticeUrl
    {
        [Test, MoqAutoData]
        public void Then_If_The_ApprenticeUrl_Is_Available_The_Url_Is_Created_If_Supplied(
            uint ukPrn,
            Guid reservationId,
            string accountLegalEntityPublicHashedId,
            DateTime startDate,
            string courseId,
            [Frozen] Mock<IOptions<ReservationsWebConfiguration>> mockConfig,
            ProviderExternalUrlHelper urlHelper)
        {
            var actualUrl = urlHelper.GenerateAddApprenticeUrl(
                ukPrn, 
                reservationId, 
                accountLegalEntityPublicHashedId, 
                startDate,
                courseId);
            
            Assert.AreEqual(
                $"{mockConfig.Object.Value.ApprenticeUrl}/{ukPrn}/unapproved/add-apprentice?reservationId={reservationId}&employerAccountLegalEntityPublicHashedId={accountLegalEntityPublicHashedId}&startMonthYear={startDate:MMyyyy}&courseCode={courseId}", 
                actualUrl);
        }

        [Test, MoqAutoData]
        public void Then_The_ApprenticeUrl_Is_Built_With_No_Course_Supplied(
            uint ukPrn,
            Guid reservationId,
            string accountLegalEntityPublicHashedId,
            DateTime startDate,
            string courseId,
            [Frozen] Mock<IOptions<ReservationsWebConfiguration>> mockConfig,
            ProviderExternalUrlHelper urlHelper)
        {
            var actualUrl = urlHelper.GenerateAddApprenticeUrl(
                ukPrn, 
                reservationId, 
                accountLegalEntityPublicHashedId, 
                startDate,
                null);

            Assert.AreEqual(
                $"{mockConfig.Object.Value.ApprenticeUrl}/{ukPrn}/unapproved/add-apprentice?reservationId={reservationId}&employerAccountLegalEntityPublicHashedId={accountLegalEntityPublicHashedId}&startMonthYear={startDate:MMyyyy}", 
                actualUrl);
        }
    }
}