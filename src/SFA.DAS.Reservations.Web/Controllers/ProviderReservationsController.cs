﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Reservations.Application.Employers.Queries;
using SFA.DAS.Reservations.Application.Reservations.Commands.CacheReservationEmployer;
using SFA.DAS.Reservations.Web.Infrastructure;
using SFA.DAS.Reservations.Web.Models;

namespace SFA.DAS.Reservations.Web.Controllers
{
    [Authorize(Policy = nameof(PolicyNames.HasProviderAccount))]
    [Route("{ukPrn}/reservations")]
    public class ProviderReservationsController : Controller
    {
        private readonly IMediator _mediator;

        public ProviderReservationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("chooseEmployer", Name = RouteNames.ProviderChooseEmployer)]
        public async Task<IActionResult> ChooseEmployer(uint ukPrn)
        {
            var employers = await _mediator.Send(new GetTrustedEmployersQuery {UkPrn = ukPrn});

            var viewModel = new ChooseEmployerViewModel
            {
                Employers = employers.Employers
            };

            return View(viewModel);
        }

        [HttpGet]
        [Route("confirmEmployer")]
        public IActionResult ConfirmEmployer(ConfirmEmployerViewModel viewModel)
        {
            return View(viewModel);
        }

        [HttpPost]
        [Route("confirmEmployer")]
        public async Task<IActionResult> ProcessConfirmEmployer(ConfirmEmployerViewModel viewModel)
        {
            if (!viewModel.Confirm.HasValue)
            {
                ModelState.AddModelError("confirm-yes", "You must pick an option");
                return View("ConfirmEmployer", viewModel);
            }

            try
            {
                if (!viewModel.Confirm.Value)
                {
                    return RedirectToAction("ChooseEmployer", "ProviderReservations", new
                    {
                        UkPrn = viewModel.UkPrn
                    });
                }

                var reservationId = Guid.NewGuid();

                await _mediator.Send(new CacheReservationEmployerCommand
                {
                    Id = reservationId,
                    AccountId = viewModel.AccountId,
                    AccountLegalEntityId = viewModel.AccountLegalEntityId,
                    AccountLegalEntityName = viewModel.AccountLegalEntityName,
                    AccountLegalEntityPublicHashedId = viewModel.AccountLegalEntityPublicHashedId
                });

                return RedirectToAction("ApprenticeshipTraining", "Reservations", new
                {
                    Id = reservationId,
                    EmployerAccountId = viewModel.AccountPublicHashedId,
                    UkPrn = viewModel.UkPrn
                });

            }
            catch (ValidationException e)
            {
                foreach (var member in e.ValidationResult.MemberNames)
                {
                    ModelState.AddModelError(member.Split('|')[0], member.Split('|')[1]);
                }

                return View("ConfirmEmployer", viewModel);
            }
        }
    }
}