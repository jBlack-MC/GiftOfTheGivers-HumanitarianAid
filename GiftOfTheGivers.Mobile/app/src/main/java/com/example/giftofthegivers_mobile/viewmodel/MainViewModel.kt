package com.example.giftofthegivers_mobile.viewmodel

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import com.example.giftofthegivers_mobile.data.*
import java.text.SimpleDateFormat
import java.util.*

class MainViewModel : ViewModel() {

    var currentScreen by mutableStateOf<Screen>(Screen.Intro)
        private set

    var currentUserRole by mutableStateOf(UserRole.Public)
        private set

    var currentUserEmail by mutableStateOf("visitor@giftofthegivers.org")
        private set

    var projects by mutableStateOf<List<ReliefProject>>(MockRepository.initialProjects)
        private set

    var donations by mutableStateOf<List<Donation>>(MockRepository.initialDonations)
        private set

    var volunteers by mutableStateOf<List<VolunteerApplication>>(MockRepository.initialVolunteers)
        private set

    var inquiries by mutableStateOf<List<Inquiry>>(MockRepository.initialInquiries)
        private set

    var auditLogs by mutableStateOf<List<AuditLogEntry>>(MockRepository.initialAuditLogs)
        private set

    var impactStats by mutableStateOf(MockRepository.getImpactStats())
        private set

    var selectedProjectIdForDonation by mutableStateOf<String?>(null)
        private set

    var lastCreatedDonation by mutableStateOf<Donation?>(null)

    fun navigateTo(screen: Screen) {
        currentScreen = screen
    }

    fun setUserRole(role: UserRole, email: String) {
        currentUserRole = role
        currentUserEmail = email
    }

    fun prepareDonation(projectId: String) {
        selectedProjectIdForDonation = projectId
        currentScreen = Screen.Donate
    }

    fun submitDonation(
        projectId: String,
        amount: Double,
        currency: String,
        donorName: String,
        donorEmail: String,
        consentGiven: Boolean
    ): Donation? {
        if (amount <= 0 || !consentGiven) return null

        val project = projects.find { it.id == projectId } ?: projects.first()
        val exchangeRate = when (currency) {
            "USD" -> 18.50
            "EUR" -> 20.00
            else -> 1.00
        }
        val amountZar = kotlin.math.round(amount * exchangeRate * 100) / 100.0
        val reference = "GOTG-${(100000..999999).random()}"
        val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.getDefault())
        val timestamp = dateFormat.format(Date())

        val newDonation = Donation(
            id = "don-${System.currentTimeMillis()}",
            reference = reference,
            projectId = project.id,
            projectName = project.title,
            amount = amount,
            currency = currency,
            amountZar = amountZar,
            paymentStatus = PaymentStatus.Pending,
            donorName = if (donorName.isBlank()) "Anonymous Donor" else donorName,
            donorEmail = if (donorEmail.isBlank()) "anon@example.com" else donorEmail,
            createdAt = timestamp
        )

        donations = listOf(newDonation) + donations
        lastCreatedDonation = newDonation

        val newAudit = AuditLogEntry(
            id = "audit-${System.currentTimeMillis()}",
            action = "DonationCaptured",
            details = "Donation $reference of $currency $amount (R $amountZar) captured as Pending.",
            actor = donorEmail,
            timestamp = timestamp
        )
        auditLogs = listOf(newAudit) + auditLogs

        currentScreen = Screen.Receipt
        return newDonation
    }

    fun simulatePayFastComplete(donationId: String) {
        val donation = donations.find { it.id == donationId } ?: return
        if (donation.paymentStatus == PaymentStatus.Verified) return

        donations = donations.map {
            if (it.id == donationId) it.copy(paymentStatus = PaymentStatus.Verified) else it
        }

        projects = projects.map { proj ->
            if (proj.id == donation.projectId) {
                proj.copy(raisedAmount = proj.raisedAmount + donation.amountZar)
            } else proj
        }

        impactStats = MockRepository.getImpactStats()

        val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.getDefault())
        val timestamp = dateFormat.format(Date())
        val newAudit = AuditLogEntry(
            id = "audit-${System.currentTimeMillis()}",
            action = "PaymentVerified",
            details = "Donation ${donation.reference} of R ${donation.amountZar} verified successfully.",
            actor = "PayFast Webhook",
            timestamp = timestamp
        )
        auditLogs = listOf(newAudit) + auditLogs

        if (lastCreatedDonation?.id == donationId) {
            lastCreatedDonation = lastCreatedDonation?.copy(paymentStatus = PaymentStatus.Verified)
        }
    }

    fun submitVolunteer(
        name: String,
        email: String,
        phone: String,
        city: String,
        skills: String,
        availability: String,
        consentGiven: Boolean
    ): Boolean {
        if (name.isBlank() || email.isBlank() || !consentGiven) return false

        val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
        val timestamp = dateFormat.format(Date())

        val newVol = VolunteerApplication(
            id = "vol-${System.currentTimeMillis()}",
            name = name,
            email = email,
            phone = phone,
            city = city,
            skills = skills,
            availability = availability,
            status = VolunteerStatus.Pending,
            consentGiven = consentGiven,
            createdAt = timestamp
        )

        volunteers = listOf(newVol) + volunteers
        return true
    }

    fun submitInquiry(
        name: String,
        email: String,
        subject: String,
        message: String,
        consentGiven: Boolean
    ): Boolean {
        if (name.isBlank() || email.isBlank() || message.isBlank() || !consentGiven) return false

        val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.getDefault())
        val timestamp = dateFormat.format(Date())

        val newInq = Inquiry(
            id = "inq-${System.currentTimeMillis()}",
            name = name,
            email = email,
            subject = subject,
            message = message,
            isHandled = false,
            createdAt = timestamp
        )

        inquiries = listOf(newInq) + inquiries
        return true
    }

    fun approveVolunteer(id: String) {
        volunteers = volunteers.map { if (it.id == id) it.copy(status = VolunteerStatus.Approved) else it }
    }

    fun rejectVolunteer(id: String) {
        volunteers = volunteers.map { if (it.id == id) it.copy(status = VolunteerStatus.Rejected) else it }
    }

    fun markInquiryHandled(id: String) {
        inquiries = inquiries.map { if (it.id == id) it.copy(isHandled = true) else it }
    }
}

sealed class Screen {
    object Intro : Screen()
    object Home : Screen()
    object Projects : Screen()
    object Donate : Screen()
    object Receipt : Screen()
    object Volunteer : Screen()
    object DonorPortal : Screen()
    object StaffPortal : Screen()
    object Privacy : Screen()
}
