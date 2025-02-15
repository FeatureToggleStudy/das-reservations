﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.Common.Domain.Types;
using SFA.DAS.Reservations.Application.Employers.Queries;
using SFA.DAS.Reservations.Application.Employers.Queries.GetLegalEntities;
using SFA.DAS.Reservations.Domain.Employers;
using SFA.DAS.Reservations.Web.Controllers;
using SFA.DAS.Reservations.Web.Models;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.Reservations.Web.UnitTests.Providers
{
    [TestFixture]
    public class WhenCallingChooseEmployer
    {
        [Test, MoqAutoData]
        public async Task Then_It_Calls_ProviderPermissions_Service_To_Get_Employers(
            ReservationsRouteModel routeModel,
            IEnumerable<Employer> expectedEmployers,
            GetLegalEntitiesResponse getLegalEntitiesResponse,
            [Frozen] Mock<IMediator> mockMediator,
            ProviderReservationsController controller)
        {
            mockMediator
                .Setup(mediator => mediator.Send(
                    It.IsAny<GetLegalEntitiesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(getLegalEntitiesResponse);
            mockMediator
                .Setup(m => m.Send(It.IsAny<GetTrustedEmployersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse{Employers = expectedEmployers});
   
            await controller.ChooseEmployer(routeModel);

            mockMediator.Verify(m => m.Send(It.IsAny<GetTrustedEmployersQuery>(),  It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, MoqAutoData]
        public async Task Then_It_Returns_The_Trusted_Employers(
            ReservationsRouteModel routeModel,
            IEnumerable<Employer> expectedEmployers,
            [Frozen] Mock<IMediator> mockMediator,
            ProviderReservationsController controller)
        {
            var getLegalEntitiesResponse = new GetLegalEntitiesResponse
            {
                AccountLegalEntities = expectedEmployers.Select(arg => new AccountLegalEntity
                {
                    AccountLegalEntityPublicHashedId = arg.AccountLegalEntityPublicHashedId,
                    AccountId = arg.AccountId,
                    IsLevy = false,
                    AgreementType = AgreementType.NonLevyExpressionOfInterest
                })
            };
            mockMediator
                .Setup(mediator => mediator.Send(
                    It.IsAny<GetLegalEntitiesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(getLegalEntitiesResponse);
            mockMediator
                .Setup(service => service.Send(It.IsAny<GetTrustedEmployersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse{Employers = expectedEmployers});
            
            var result = await controller.ChooseEmployer(routeModel);

            var viewModel = result.Should().BeOfType<ViewResult>()
                .Which.Model.Should().BeOfType<ChooseEmployerViewModel>()
                .Subject;
            viewModel.Employers.Should().BeEquivalentTo(expectedEmployers);
        }

        [Test, MoqAutoData]
        public async Task Then_It_Filters_NonLevy_NonEoi_Employers(
            ReservationsRouteModel routeModel,
            List<Employer> expectedEmployers,
            [Frozen] Mock<IMediator> mockMediator,
            ProviderReservationsController controller)
        {
            var getLegalEntitiesResponse = new GetLegalEntitiesResponse
            {
                AccountLegalEntities = expectedEmployers.Select(arg => new AccountLegalEntity
                {
                    AccountLegalEntityPublicHashedId = arg.AccountLegalEntityPublicHashedId,
                    AccountId = arg.AccountId,
                    IsLevy = false,
                    AgreementType = AgreementType.Levy
                })
            };
            mockMediator
                .Setup(mediator => mediator.Send(
                    It.IsAny<GetLegalEntitiesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(getLegalEntitiesResponse);
            mockMediator
                .Setup(service => service.Send(It.IsAny<GetTrustedEmployersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse{Employers = expectedEmployers});
            
            var result = await controller.ChooseEmployer(routeModel) as ViewResult;

            var viewModel = result.Model as ChooseEmployerViewModel;
            viewModel.Employers.Count().Should().Be(0);
        }
    }
}