package com.example.giftofthegivers_mobile.data

data class ReliefProject(
    val id: String,
    val title: String,
    val description: String,
    val location: String,
    val status: ProjectStatus,
    val targetAmount: Double,
    val raisedAmount: Double,
    val offlineFunds: Double = 0.0,
    val category: String,
    val imageUrl: String = ""
) {
    val totalRaised: Double get() = raisedAmount + offlineFunds
    val progressPercentage: Float get() = if (targetAmount > 0) (totalRaised / targetAmount).toFloat().coerceIn(0f, 1f) else 0f
}

enum class ProjectStatus {
    Active, Suspended, Completed
}

data class Donation(
    val id: String,
    val reference: String,
    val projectId: String,
    val projectName: String,
    val amount: Double,
    val currency: String,
    val amountZar: Double,
    val paymentStatus: PaymentStatus,
    val donorName: String,
    val donorEmail: String,
    val createdAt: String
)

enum class PaymentStatus {
    Pending, Verified, Failed
}

data class VolunteerApplication(
    val id: String,
    val name: String,
    val email: String,
    val phone: String,
    val city: String,
    val skills: String,
    val availability: String,
    val status: VolunteerStatus,
    val consentGiven: Boolean,
    val createdAt: String
)

enum class VolunteerStatus {
    Pending, Approved, Rejected
}

data class ImpactStats(
    val peopleHelped: Int,
    val projectsFunded: Int,
    val waterBoreholesDug: Int,
    val totalRaisedZar: Double
)

data class Inquiry(
    val id: String,
    val name: String,
    val email: String,
    val subject: String,
    val message: String,
    val isHandled: Boolean,
    val createdAt: String
)

data class AuditLogEntry(
    val id: String,
    val action: String,
    val details: String,
    val actor: String,
    val timestamp: String
)

enum class UserRole {
    Public, Donor, Employee
}
