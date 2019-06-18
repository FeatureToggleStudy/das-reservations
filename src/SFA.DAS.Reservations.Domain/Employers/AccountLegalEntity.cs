﻿namespace SFA.DAS.Reservations.Domain.Employers
{
    public class AccountLegalEntity
    {
        public string AccountId { get; set; }
        public long LegalEntityId { get; set; }
        public string AccountLegalEntityName { get; set; }
        public long AccountLegalEntityId { get; set; }
        public string AccountLegalEntityPublicHashedId { get; set; }
        public int ReservationLimit { get; set; }
    }
}