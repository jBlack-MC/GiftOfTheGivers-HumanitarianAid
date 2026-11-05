package com.example.giftofthegivers_mobile

import com.example.giftofthegivers_mobile.data.PaymentStatus
import com.example.giftofthegivers_mobile.data.UserRole
import com.example.giftofthegivers_mobile.viewmodel.MainViewModel
import com.example.giftofthegivers_mobile.viewmodel.Screen
import org.junit.Assert.*
import org.junit.Test

class MainViewModelTest {

    @Test
    fun testInitialState() {
        val viewModel = MainViewModel()
        assertEquals(Screen.Intro, viewModel.currentScreen)
        assertEquals(UserRole.Public, viewModel.currentUserRole)
        assertTrue(viewModel.projects.isNotEmpty())
    }

    @Test
    fun testNavigation() {
        val viewModel = MainViewModel()
        viewModel.navigateTo(Screen.Home)
        assertEquals(Screen.Home, viewModel.currentScreen)
    }

    @Test
    fun testDonationCaptureAndCurrencyConversion() {
        val viewModel = MainViewModel()
        val project = viewModel.projects.first()

        // USD 100 at 18.50 rate = R 1,850.00
        val donation = viewModel.submitDonation(
            projectId = project.id,
            amount = 100.0,
            currency = "USD",
            donorName = "Test Donor",
            donorEmail = "test@example.com",
            consentGiven = true
        )

        assertNotNull(donation)
        assertEquals(1850.0, donation!!.amountZar, 0.01)
        assertEquals(PaymentStatus.Pending, donation.paymentStatus)
        assertEquals(Screen.Receipt, viewModel.currentScreen)
    }

    @Test
    fun testVolunteerApplicationSubmission() {
        val viewModel = MainViewModel()
        val success = viewModel.submitVolunteer(
            name = "Jane Doe",
            email = "jane@example.com",
            phone = "+27 82 123 4567",
            city = "Durban",
            skills = "Medical, Nursing",
            availability = "Weekdays",
            consentGiven = true
        )

        assertTrue(success)
        assertTrue(viewModel.volunteers.any { it.email == "jane@example.com" })
    }
}
