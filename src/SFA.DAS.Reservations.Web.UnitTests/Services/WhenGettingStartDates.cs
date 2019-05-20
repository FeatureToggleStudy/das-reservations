﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.Reservations.Application.FundingRules.Queries.GetAvailableDates;
using SFA.DAS.Reservations.Domain.Rules;
using SFA.DAS.Reservations.Web.Services;

namespace SFA.DAS.Reservations.Web.UnitTests.Services
{
    [TestFixture]
    public class WhenGettingStartDates
    {
        [Test, MoqAutoData]
        public async Task Then_Gets_The_Available_Dates_From_The_Query(
            [Frozen] Mock<IMediator> mockMediator,
            IList<StartDateModel> expectedAvailableDates,
            StartDateService startDateService)
        {
            mockMediator.Setup(x => x.Send(It.IsAny<GetAvailableDatesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetAvailableDatesResult
                {
                    AvailableDates = expectedAvailableDates
                });

            await startDateService.GetStartDates(1);

            mockMediator.Verify(x=>x.Send(It.IsAny<GetAvailableDatesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, MoqAutoData]
        public async Task Then_The_Returned_Dates_Are_Mapped_To_The_Model(
            [Frozen] Mock<IMediator> mockMediator,
            IList<StartDateModel> expectedAvailableDates,
            StartDateService startDateService)
        {
            mockMediator.Setup(x => x.Send(It.IsAny<GetAvailableDatesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetAvailableDatesResult
                {
                    AvailableDates = expectedAvailableDates
                });
            

            var dates = await startDateService.GetStartDates(1);

            Assert.IsNotNull(dates);
            Assert.AreEqual(expectedAvailableDates.Count,dates.Count());

        }
    }
}