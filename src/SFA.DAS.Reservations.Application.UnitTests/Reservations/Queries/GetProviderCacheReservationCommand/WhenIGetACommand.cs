﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.Reservations.Application.Employers.Queries;
using SFA.DAS.Reservations.Application.Exceptions;
using SFA.DAS.Reservations.Application.Providers.Queries.GetLegalEntityAccount;
using SFA.DAS.Reservations.Application.Reservations.Queries.GetProviderCacheReservationCommand;
using SFA.DAS.Reservations.Application.Validation;
using SFA.DAS.Reservations.Domain.Employers;
using ValidationResult = SFA.DAS.Reservations.Application.Validation.ValidationResult;

namespace SFA.DAS.Reservations.Application.UnitTests.Reservations.Queries.GetProviderCacheReservationCommand
{
    public class WhenIGetACommand
    {
        private Mock<IMediator> _mediator;
        private Mock<IValidator<GetProviderCacheReservationCommandQuery>> _validator;
        private GetProviderCacheReservationCommandQueryHandler _handler;
        private GetProviderCacheReservationCommandQuery _query;
        private GetTrustedEmployersResponse _getTrustedEmployersResponse;
        private GetAccountLegalEntityResult _getAccountLegalEntityResponse;
        private Employer _expectedEmployer;
        private AccountLegalEntity _expectedAccountLegalEntity;

        [SetUp]
        public void Arrange()
        {
            _mediator = new Mock<IMediator>();
            _validator = new Mock<IValidator<GetProviderCacheReservationCommandQuery>>();
            _query = new GetProviderCacheReservationCommandQuery
            {
                UkPrn = 12,
                AccountLegalEntityPublicHashedId = "ABC123",
                CohortRef = "1234"
            };

            _handler = new GetProviderCacheReservationCommandQueryHandler(_mediator.Object, _validator.Object);

            _expectedEmployer = new Employer
            {
                AccountId = 1,
                AccountLegalEntityPublicHashedId = _query.AccountLegalEntityPublicHashedId,
                AccountLegalEntityName = "Test Employer",
                AccountLegalEntityId = 123,
                AccountName = "Test Account",
                AccountPublicHashedId = "DEF123"
            };

            _expectedAccountLegalEntity = new AccountLegalEntity
            {
                AccountId = "1",
                AccountLegalEntityPublicHashedId = _query.AccountLegalEntityPublicHashedId,
                AccountLegalEntityName = "Test Employer",
                AccountLegalEntityId = 123,
                LegalEntityId = 456,
                ReservationLimit = 3
            };

            _getTrustedEmployersResponse = new GetTrustedEmployersResponse
            {
                Employers = new [] { _expectedEmployer }
            };

            _getAccountLegalEntityResponse = new GetAccountLegalEntityResult
            {
                LegalEntity = _expectedAccountLegalEntity
            };

            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetTrustedEmployersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_getTrustedEmployersResponse);

            _mediator.Setup(mediator => mediator.Send(
                            It.IsAny<GetAccountLegalEntityQuery>(),
                            It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_getAccountLegalEntityResponse);

            _validator.Setup(v => v.ValidateAsync(_query))
                .ReturnsAsync(new ValidationResult(){ValidationDictionary = new Dictionary<string, string>()});
        }

        [Test]
        public async Task ThenAProvidedTrustedEmployersAreRetrieved()
        {
            //Act
            await _handler.Handle(_query, CancellationToken.None);

            //Assert
            _mediator.Verify(mediator => mediator.Send(
                    It.Is<GetTrustedEmployersQuery>(query => query.UkPrn == _query.UkPrn),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ThenAMatchedTrustedEmployerDetailsAreReturnInCommand()
        {
            //Act
            var result = await _handler.Handle(_query, CancellationToken.None);

            //Assert
            Assert.IsNotNull(result.Command);
            Assert.AreEqual(_expectedEmployer.AccountLegalEntityName, result.Command.AccountLegalEntityName);
            Assert.AreEqual(_expectedEmployer.AccountLegalEntityPublicHashedId, result.Command.AccountLegalEntityPublicHashedId);
            Assert.AreEqual(_expectedEmployer.AccountLegalEntityId, result.Command.AccountLegalEntityId);
            Assert.AreEqual(_expectedEmployer.AccountId, result.Command.AccountId);
            Assert.AreEqual(_expectedEmployer.AccountName, result.Command.AccountName);
        }

        [Test]
        public async Task ThenIfNoTrustedEmployerIsFoundThenTheAccountLegalEntityIsRetrieved()
        {
            //Arrange
            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetTrustedEmployersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse
                {
                    Employers = new Employer[0]
                });

            //Act
            await _handler.Handle(_query, CancellationToken.None);

            //Assert
            _mediator.Verify(mediator => mediator.Send(
                    It.Is<GetAccountLegalEntityQuery>(q => 
                        q.AccountLegalEntityPublicHashedId.Equals(_query.AccountLegalEntityPublicHashedId)),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ThenIfNoTrustedEmployerIsFoundThenTheLegalEntityAccountDetailsAreUsed()
        {
            //Arrange
            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetTrustedEmployersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse
                {
                    Employers = new Employer[0]
                });

            //Act
            var result = await _handler.Handle(_query, CancellationToken.None);

            //Assert
            Assert.IsNotNull(result.Command);
            Assert.AreEqual(_expectedAccountLegalEntity.AccountLegalEntityName, result.Command.AccountLegalEntityName);
            Assert.AreEqual(_expectedAccountLegalEntity.AccountLegalEntityPublicHashedId, result.Command.AccountLegalEntityPublicHashedId);
            Assert.AreEqual(_expectedAccountLegalEntity.AccountLegalEntityId, result.Command.AccountLegalEntityId);
            Assert.AreEqual(_expectedAccountLegalEntity.AccountId, result.Command.AccountId.ToString());
        }

        [Test]
        public void ThenNeitherTheTrustedEmployerOrAccountLegalEntityIsFoundAnExceptionIsThrown()
        {
            //Arrange
            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetTrustedEmployersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse
                {
                    Employers = new Employer[0]
                });

            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetAccountLegalEntityQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetAccountLegalEntityResult());

            //Act + Assert
            Assert.ThrowsAsync<AccountLegalEntityNotFoundException>(() => _handler.Handle(_query, CancellationToken.None));
        }

        [Test]
        public void ThenIfTheRetrievedLegalEntityHasAnInvalidIdAnExceptionIsThrown()
        {
            //Arrange
            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetTrustedEmployersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTrustedEmployersResponse
                {
                    Employers = new Employer[0]
                });
            
            _expectedAccountLegalEntity.AccountId = "Not a valid ID";

            //Act + Assert
            Assert.ThrowsAsync<AccountLegalEntityInvalidException>(() => _handler.Handle(_query, CancellationToken.None));
        }

        [Test]
        public void ThenIfValidationFailsAnExceptionIsThrown()
        {
            //Arrange
            _validator.Setup(v => v.ValidateAsync(_query))
                .ReturnsAsync(new ValidationResult{ValidationDictionary = new Dictionary<string, string>{{"Error", "Test Error"}}});

            //Act + Assert
            Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(_query, CancellationToken.None));
        }
    }
}
