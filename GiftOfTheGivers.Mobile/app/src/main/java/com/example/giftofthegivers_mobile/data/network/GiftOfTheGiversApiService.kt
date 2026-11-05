package com.example.giftofthegivers_mobile.data.network

import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.*

data class ReliefProjectDto(
    val id: Int,
    val title: String,
    val description: String,
    val location: String,
    val status: String,
    val fundsRequired: Double,
    val offlineFundsZar: Double,
    val fundsRaised: Double,
    val progressPercentage: Double,
    val imageUrl: String?
)

data class ImpactStatsDto(
    val activeProjects: Int,
    val completedProjects: Int,
    val approvedVolunteers: Int,
    val verifiedDonations: Int,
    val totalRaisedZar: Double
)

data class CreateDonationRequest(
    val reliefProjectId: Int?,
    val amount: Double,
    val currency: String,
    val donationType: String,
    val paymentMethod: String,
    val donorName: String,
    val donorEmail: String,
    val isAnonymous: Boolean
)

data class DonationResponseDto(
    val transactionReference: String,
    val amount: Double,
    val currency: String,
    val amountZar: Double,
    val paymentStatus: String,
    val paymentCheckoutUrl: String?
)

data class VolunteerRequest(
    val fullName: String,
    val email: String,
    val phoneNumber: String,
    val city: String,
    val skills: String,
    val availability: String,
    val emergencyContactName: String,
    val emergencyContactPhone: String,
    val privacyConsent: Boolean
)

data class InquiryRequest(
    val name: String,
    val email: String,
    val subject: String,
    val message: String,
    val privacyConsent: Boolean
)

interface GiftOfTheGiversApiService {

    @GET("api/v1/projects")
    suspend fun getProjects(@Query("status") status: String? = null): List<ReliefProjectDto>

    @GET("api/v1/stats")
    suspend fun getStats(): ImpactStatsDto

    @POST("api/v1/donations")
    suspend fun createDonation(@Body request: CreateDonationRequest): DonationResponseDto

    @POST("api/v1/volunteers")
    suspend fun applyVolunteer(@Body request: VolunteerRequest): Map<String, Any>

    @POST("api/v1/inquiries")
    suspend fun sendInquiry(@Body request: InquiryRequest): Map<String, Any>

    companion object {
        // Default base URL for Android Emulator pointing to local ASP.NET Core backend (http://localhost:5106)
        // or Azure production URL (https://giftgivers-app-bvefdybyc7baguaq.southafricanorth-01.azurewebsites.net/)
        const val BASE_URL = "http://10.0.2.2:5106/"

        fun create(): GiftOfTheGiversApiService {
            return Retrofit.Builder()
                .baseUrl(BASE_URL)
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(GiftOfTheGiversApiService::class.java)
        }
    }
}
