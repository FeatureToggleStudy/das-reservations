﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SFA.DAS.Reservations.Application.Reservations.Commands.CacheReservationCourse;
using SFA.DAS.Reservations.Application.Reservations.Queries.GetCachedReservation;
using SFA.DAS.Reservations.Application.Reservations.Queries.GetCourses;
using SFA.DAS.Reservations.Application.Reservations.Services;
using SFA.DAS.Reservations.Domain.Courses;
using SFA.DAS.Reservations.Web.Controllers;
using SFA.DAS.Reservations.Web.Models;

namespace SFA.DAS.Reservations.Web.UnitTests.Employers
{
    public class WhenCallingPostSelectCourse
    {
        private Course _course;
        private GetCachedReservationResult _cachedReservationResult;
        private Mock<IMediator> _mediator;
        private Mock<IHashingService> _hashingService;
        private EmployerReservationsController _controller;

        [SetUp]
        public void Arrange()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            _course = new Course("1-4-5", "test", 1);
            _cachedReservationResult = fixture.Create<GetCachedReservationResult>();
            _hashingService = new Mock<IHashingService>();

            _mediator = new Mock<IMediator>();
            _controller = new EmployerReservationsController(_mediator.Object,_hashingService.Object);

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
        public async Task Then_Caches_Draft_Reservation(ReservationsRouteModel routeModel)
        {
            //Assign
            var selectedCourse = _course.Id;

            //Act
            await _controller.PostSelectCourse(routeModel, selectedCourse);

            //Assert
            _mediator.Verify(mediator => 
                mediator.Send(It.Is<CacheReservationCourseCommand>(command => 
                    command.CourseId.Equals(_course.Id)), It.IsAny<CancellationToken>()));
        }

        [Test, MoqAutoData] 
        public async Task And_No_Course_Then_Caches_Draft_Reservation(ReservationsRouteModel routeModel)
        {
            _mediator.Setup(mediator => mediator.Send(
                    It.IsAny<GetCachedReservationQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _cachedReservationResult);

            await _controller.PostSelectCourse(routeModel, null);

            _mediator.Verify(mediator => 
                mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Test, MoqAutoData]
        public async Task Then_Adds_Guid_To_RouteModel(ReservationsRouteModel routeModel)
        {
            //Assign
            var selectedCourse = _course.Id;

            //Act
            var result = await _controller.PostSelectCourse(routeModel, selectedCourse) as RedirectToRouteResult;

            //Assert
            Assert.IsNotNull(result);

            result.RouteValues.Should().ContainKey("Id")
                .WhichValue.Should().Be(routeModel.Id);
        }

        [Test, AutoData]//note cannot use moqautodata to construct controller here due to modelmetadata usage.
        public async Task And_Validation_Error_Then_Returns_Validation_Error_Details(ReservationsRouteModel routeModel)
        {
            //Assign
            var selectedCourse = _course.Id;

            _mediator.Setup(mediator => mediator.Send(It.IsAny<CacheReservationCourseCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new ValidationResult("Failed", new List<string> { "Course|The Course field is not valid." }), null, null));

            //Act
            var result = await _controller.PostSelectCourse(routeModel, selectedCourse);

            //Assert
            Assert.IsNotNull(result);
            var actualViewResult = result as ViewResult;
            Assert.IsNotNull(actualViewResult);
            Assert.IsFalse(actualViewResult.ViewData.ModelState.IsValid);
            Assert.IsTrue(actualViewResult.ViewData.ModelState.ContainsKey("Course"));
        }
    }
}