﻿using System;

namespace SFA.DAS.Reservations.Application.Reservations.Queries
{
    public class GetReservationResult
    {
        public Guid ReservationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}