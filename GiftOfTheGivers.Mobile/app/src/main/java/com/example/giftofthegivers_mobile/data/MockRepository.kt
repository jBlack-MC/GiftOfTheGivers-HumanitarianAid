package com.example.giftofthegivers_mobile.data

object MockRepository {

    val initialProjects = listOf(
        ReliefProject(
            id = "proj-1",
            title = "Gaza Emergency Medical & Food Relief",
            description = "Providing urgent medical supplies, trauma care, food parcels, and clean water access to affected families in Gaza.",
            location = "Gaza, Middle East",
            status = ProjectStatus.Active,
            targetAmount = 5000000.0,
            raisedAmount = 3450000.0,
            offlineFunds = 250000.0,
            category = "Medical & Food"
        ),
        ReliefProject(
            id = "proj-2",
            title = "Eastern Cape Drought & Water Provision",
            description = "Drilling and equipping deep groundwater boreholes and distributing water tankers to rural communities facing severe drought.",
            location = "Eastern Cape, South Africa",
            status = ProjectStatus.Active,
            targetAmount = 2000000.0,
            raisedAmount = 1680000.0,
            offlineFunds = 120000.0,
            category = "Water & Sanitation"
        ),
        ReliefProject(
            id = "proj-3",
            title = "KZN Flood Recovery & Housing Assistance",
            description = "Rebuilding homes, supplying building materials, and offering psychosocial support to families displaced by severe flooding.",
            location = "KwaZulu-Natal, South Africa",
            status = ProjectStatus.Active,
            targetAmount = 3500000.0,
            raisedAmount = 2100000.0,
            offlineFunds = 300000.0,
            category = "Disaster Relief"
        ),
        ReliefProject(
            id = "proj-4",
            title = "Winter Warmth & Blanket Distribution",
            description = "Distributing warm blankets, heavy-duty winter clothing, and nutritional soup kitchen supplies to vulnerable shelters.",
            location = "National, South Africa",
            status = ProjectStatus.Active,
            targetAmount = 1000000.0,
            raisedAmount = 890000.0,
            offlineFunds = 50000.0,
            category = "Winter Relief"
        ),
        ReliefProject(
            id = "proj-5",
            title = "Back to School Education & Stationery Drive",
            description = "Equipping underprivileged learners with school uniforms, backpacks, and complete stationery sets for the academic year.",
            location = "Nationwide, South Africa",
            status = ProjectStatus.Active,
            targetAmount = 1500000.0,
            raisedAmount = 1250000.0,
            offlineFunds = 100000.0,
            category = "Education"
        )
    )

    val initialDonations = mutableListOf(
        Donation(
            id = "don-1",
            reference = "GOTG-984210",
            projectId = "proj-1",
            projectName = "Gaza Emergency Medical & Food Relief",
            amount = 1000.0,
            currency = "ZAR",
            amountZar = 1000.0,
            paymentStatus = PaymentStatus.Verified,
            donorName = "John Doe",
            donorEmail = "donor@example.com",
            createdAt = "2026-11-01 14:32"
        ),
        Donation(
            id = "don-2",
            reference = "GOTG-753190",
            projectId = "proj-2",
            projectName = "Eastern Cape Drought & Water Provision",
            amount = 100.0,
            currency = "USD",
            amountZar = 1850.0,
            paymentStatus = PaymentStatus.Verified,
            donorName = "Sarah Smith",
            donorEmail = "sarah@example.com",
            createdAt = "2026-11-02 09:15"
        ),
        Donation(
            id = "don-3",
            reference = "GOTG-362814",
            projectId = "proj-3",
            projectName = "KZN Flood Recovery & Housing Assistance",
            amount = 500.0,
            currency = "EUR",
            amountZar = 10000.0,
            paymentStatus = PaymentStatus.Pending,
            donorName = "Anonymous Donor",
            donorEmail = "anon@example.com",
            createdAt = "2026-11-02 16:40"
        )
    )

    val initialVolunteers = mutableListOf(
        VolunteerApplication(
            id = "vol-1",
            name = "Thabo Mokoena",
            email = "thabo@example.com",
            phone = "+27 82 555 1234",
            city = "Johannesburg",
            skills = "Paramedic, First Aid, Logistics",
            availability = "Weekends and Evenings",
            status = VolunteerStatus.Approved,
            consentGiven = true,
            createdAt = "2026-10-25"
        ),
        VolunteerApplication(
            id = "vol-2",
            name = "Lisa van der Merwe",
            email = "lisa@example.com",
            phone = "+27 83 555 9876",
            city = "Cape Town",
            skills = "Administration, Counseling, Teaching",
            availability = "Full-time",
            status = VolunteerStatus.Pending,
            consentGiven = true,
            createdAt = "2026-11-01"
        )
    )

    val initialInquiries = mutableListOf(
        Inquiry(
            id = "inq-1",
            name = "David Miller",
            email = "david@example.com",
            subject = "Corporate Partnership",
            message = "We would like to discuss a corporate donation matching program for our employees.",
            isHandled = false,
            createdAt = "2026-11-02 11:20"
        )
    )

    val initialAuditLogs = mutableListOf(
        AuditLogEntry(
            id = "audit-1",
            action = "PaymentVerified",
            details = "Donation GOTG-984210 of R 1,000.00 verified via PayFast ITN.",
            actor = "PayFast Webhook",
            timestamp = "2026-11-01 14:33"
        ),
        AuditLogEntry(
            id = "audit-2",
            action = "VolunteerApproved",
            details = "Volunteer application vol-1 (Thabo Mokoena) approved.",
            actor = "employee@giftofthegivers.org",
            timestamp = "2026-10-26 10:00"
        )
    )

    fun getImpactStats(): ImpactStats {
        val totalZar = initialDonations.filter { it.paymentStatus == PaymentStatus.Verified }.sumOf { it.amountZar } +
                initialProjects.sumOf { it.raisedAmount + it.offlineFunds }
        return ImpactStats(
            peopleHelped = 1450280,
            projectsFunded = initialProjects.size,
            waterBoreholesDug = 840,
            totalRaisedZar = totalZar
        )
    }
}
