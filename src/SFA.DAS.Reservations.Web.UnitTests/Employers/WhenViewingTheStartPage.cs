﻿using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.Reservations.Application.Reservations.Commands.CacheReservationEmployer;
using SFA.DAS.Reservations.Application.Reservations.Services;
using SFA.DAS.Reservations.Web.Controllers;
using SFA.DAS.Reservations.Web.Models;

namespace SFA.DAS.Reservations.Web.UnitTests.Employers
{
    public class WhenViewingTheStartPage
    {
        [Test, MoqAutoData]
        public async Task Then_Reservation_Cache_WIll_Be_Created(
            [Frozen] Mock<IMediator> mockMediator,
            [Frozen] Mock<IHashingService> mockHashingService,
            EmployerReservationsController controller,
            ReservationsRouteModel routeModel)
        {
            //Assign
            const int expectedEmployerAccountId = 123;
            
            mockMediator.Setup(m => m.Send(It.IsAny<CacheReservationEmployerCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            mockHashingService.Setup(s => s.DecodeValue(routeModel.EmployerAccountId))
                .Returns(expectedEmployerAccountId);   
            
            //Act
            await controller.Index(routeModel);
            
            //Assert
            mockMediator.Verify(m => m.Send(It.Is<CacheReservationEmployerCommand>( c =>
                   c.AccountId.Equals(expectedEmployerAccountId) &&
                   c.AccountLegalEntityId != default(long) &&
                   !string.IsNullOrEmpty(c.AccountLegalEntityName)), It.IsAny<CancellationToken>()));
        }
    }
}