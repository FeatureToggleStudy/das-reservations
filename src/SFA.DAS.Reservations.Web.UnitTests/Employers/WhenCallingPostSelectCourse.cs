﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using NUnit.Framework;
using SFA.DAS.Reservations.Application.Reservations.Commands.CacheReservationCourse;
using SFA.DAS.Reservations.Application.Reservations.Queries.GetCachedReservation;
using SFA.DAS.Reservations.Application.Reservations.Queries.GetCourses;
using SFA.DAS.Reservations.Domain.Courses;
using SFA.DAS.Reservations.Domain.Interfaces;
using SFA.DAS.Reservations.Web.Controllers;
using SFA.DAS.Reservations.Web.Infrastructure;
using SFA.DAS.Reservations.Web.Models;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.Reservations.Web.UnitTests.Employers
{
    public class WhenCallingPostSelectCourse
    {
        private Course _course;
        private GetCachedReservationResult _cachedReservationResult;
        private Mock<IMediator> _mediator;
        private EmployerReservationsController _controller;
        private Mock<IExternalUrlHelper> _externalUrlHelper;

        [SetUp]
        public void Arrange()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization{ConfigureMembers = true});

            _course = new Course("1-4-5", "test", 1);
            _cachedReservationResult = fixture.Create<GetCachedReservationResult>();
            _externalUrlHelper = fixture.Freeze<Mock<IExternalUrlHelper>>();
            _mediator = fixture.Freeze<Mock<IMediator>>();
            
            _controller = fixture.Create<EmployerReservationsController>();

            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetCachedReservationQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _cachedReservationResult);

            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<CacheReservationCourseCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            _mediator.Setup(m => m.Send(It.IsAny<GetCoursesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCoursesResult
                {
                    Courses = new List<Course>
                    {
                        _course
                    }
                });
        }

        [Test, MoqAutoData]
        public async Task Then_Caches_Draft_Reservation(
            ReservationsRouteModel routeModel,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Assign
            postSelectCourseViewModel.SelectedCourseId = _course.Id;
            postSelectCourseViewModel.ApprenticeTrainingKnown = true;

            //Act
            await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel);

            //Assert
            _mediator.Verify(mediator => 
                mediator.Send(It.Is<CacheReservationCourseCommand>(command => 
                    command.CourseId.Equals(_course.Id)), It.IsAny<CancellationToken>()));
        }

        [Test, MoqAutoData] 
        public async Task And_No_Course_Then_Caches_Draft_Reservation(
            ReservationsRouteModel routeModel,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetCachedReservationQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _cachedReservationResult);
            postSelectCourseViewModel.SelectedCourseId = null;
            postSelectCourseViewModel.ApprenticeTrainingKnown = true;
            await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel);

            _mediator.Verify(mediator => 
                mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Test, MoqAutoData]
        public async Task Then_Adds_Guid_To_RouteModel(
            ReservationsRouteModel routeModel,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Assign
            postSelectCourseViewModel.SelectedCourseId = _course.Id;

            //Act
            var result = await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel) as RedirectToRouteResult;

            //Assert
            Assert.IsNotNull(result);

            result.RouteValues.Should().ContainKey("Id")
                .WhichValue.Should().Be(routeModel.Id);
        }

        [Test, AutoData]//note cannot use moqautodata to construct controller here due to modelmetadata usage.
        public async Task And_Validation_Error_Then_Returns_Validation_Error_Details(
            ReservationsRouteModel routeModel,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Assign
            postSelectCourseViewModel.SelectedCourseId = _course.Id;
            postSelectCourseViewModel.ApprenticeTrainingKnown = true;

            _mediator.Setup(mediator => mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new ValidationResult("Failed", new List<string> { "Course|The Course field is not valid." }), null, null));

            //Act
            var result = await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel);

            //Assert
            Assert.IsNotNull(result);
            var actualViewResult = result as ViewResult;
            Assert.That(actualViewResult.ViewName == "SelectCourse");
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState.ContainsKey("Course"));
        }
        
        [Test, MoqAutoData]
        public async Task Then_The_BackLink_Is_Set_To_Return_To_CohortDetails_From_ValidationError_If_There_Is_A_Cohort_Ref(
            ReservationsRouteModel routeModel,
            string cohortUrl,
            PostSelectCourseViewModel postSelectCourseViewModel
            )
        {
            //Arrange
            routeModel.CohortReference = "ABC123";
            _mediator.Setup(mediator => mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new ValidationResult("Failed", new List<string> { "Course|The Course field is not valid." }), null, null));
            postSelectCourseViewModel.SelectedCourseId = _course.Id;
            postSelectCourseViewModel.ApprenticeTrainingKnown = true;
            _externalUrlHelper
                .Setup(x => x.GenerateCohortDetailsUrl(null, routeModel.EmployerAccountId,routeModel.CohortReference))
                .Returns(cohortUrl);

            //Act
            var result = await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel) as ViewResult;

            var viewModel = result?.Model as EmployerSelectCourseViewModel;
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(cohortUrl, viewModel.BackLink);
            Assert.AreEqual(routeModel.CohortReference, viewModel.CohortReference);
        }

        [Test, MoqAutoData]
        public async Task Then_The_BackLink_Is_Set_To_Return_To_ReviewPage_If_There_Is_FromReview_Flag(
            ICollection<Course> courses,
            [Frozen] Mock<IMediator> mockMediator,
            ReservationsRouteModel routeModel,
            EmployerReservationsController controller,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Arrange
            routeModel.CohortReference = "";
            routeModel.FromReview = true;
            _mediator.Setup(mediator => mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new ValidationResult("Failed", new List<string> { "Course|The Course field is not valid." }), null, null));
            postSelectCourseViewModel.SelectedCourseId = _course.Id;


            //Act
            var result = await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel) as ViewResult;

            var viewModel = result?.Model as EmployerSelectCourseViewModel;
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(RouteNames.EmployerReview, viewModel.BackLink);
            Assert.AreEqual(routeModel.CohortReference, viewModel.CohortReference);
        }


        [Test, MoqAutoData]
        public async Task Then_The_BackLink_Is_Set_To_Return_To_SelectLegalEntityView(
            ICollection<Course> courses,
            [Frozen] Mock<IMediator> mockMediator,
            ReservationsRouteModel routeModel,
            EmployerReservationsController controller,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Arrange
            routeModel.CohortReference = "";
            routeModel.FromReview = false;
            _mediator.Setup(mediator => mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new ValidationResult("Failed", new List<string> { "Course|The Course field is not valid." }), null, null));
            postSelectCourseViewModel.SelectedCourseId= _course.Id;
            

            //Act
            var result = await _controller.PostSelectCourse(routeModel, postSelectCourseViewModel) as ViewResult;

            var viewModel = result?.Model as EmployerSelectCourseViewModel;
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(RouteNames.EmployerSelectLegalEntity, viewModel.BackLink);
            Assert.AreEqual(routeModel.CohortReference, viewModel.CohortReference);
        }

        [Test, MoqAutoData]
        public async Task WhenNoneOfTheRadioOptionsAreSelected_ThenModelIsInvalid(
            ReservationsRouteModel routeModel,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Arrange
            var expectedViewName = "SelectCourse";
            postSelectCourseViewModel.ApprenticeTrainingKnown = null;
            List<ValidationResult> validationResults = new List<ValidationResult>();
            var context = new ValidationContext(postSelectCourseViewModel, null, null);


            //Act 
            Validator.TryValidateObject(postSelectCourseViewModel, context, validationResults, true);

            //Assert
            Assert.True(validationResults.Count > 0);
        }

        [Test, MoqAutoData]
        public async Task WhenARadioOptionIsSelected_ThenModelIsValid(
            ReservationsRouteModel routeModel,
            PostSelectCourseViewModel postSelectCourseViewModel,
            bool choice)
        {
            //Arrange
            postSelectCourseViewModel.ApprenticeTrainingKnown = choice;
            List<ValidationResult> validationResults = new List<ValidationResult>();
            var context = new ValidationContext(postSelectCourseViewModel, null, null);


            //Act 
            Validator.TryValidateObject(postSelectCourseViewModel, context, validationResults, true);

            //Assert
            Assert.True(validationResults.Count == 0);
        }

        [Test, MoqAutoData]
        public async Task WhenApprenticeshipTrainingNotKnown_ThenRedirectsToGuidancePage(
            ReservationsRouteModel routeModel,
            EmployerReservationsController controller,
            PostSelectCourseViewModel postSelectCourseViewModel)
        {
            //Arrange
            postSelectCourseViewModel.ApprenticeTrainingKnown = false;
            var expectedRouteName = RouteNames.EmployerCourseGuidance;

            //Act
            var result = await controller.PostSelectCourse(routeModel, postSelectCourseViewModel) as RedirectToRouteResult;

            //Assert
            Assert.NotNull(result);
            Assert.AreEqual(expectedRouteName, result.RouteName);
        }
    }
}
